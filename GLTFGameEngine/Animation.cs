using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace GLTFGameEngine
{
    internal class AnimationData
    {
        public List<float[]> Translations = new();
        public List<float[]> Rotations = new();
        public List<float[]> Scales = new();

        public void AddData(glTFLoader.Schema.AnimationChannelTarget.PathEnum channelTarget, float[] data)
        {
            if (channelTarget == glTFLoader.Schema.AnimationChannelTarget.PathEnum.translation)
            {
                for (int i = 0; i < data.Length; i += 3)
                {
                    Translations.Add(new float[] { data[i], data[i + 1], data[i + 2] });
                }
            }
            if (channelTarget == glTFLoader.Schema.AnimationChannelTarget.PathEnum.scale)
            {
                for (int i = 0; i < data.Length; i += 3)
                {
                    Scales.Add(new float[] { data[i], data[i + 1], data[i + 2] });
                }
            }
            if (channelTarget == glTFLoader.Schema.AnimationChannelTarget.PathEnum.rotation)
            {
                for (int i = 0; i < data.Length; i += 4)
                {
                    Rotations.Add(new float[] { data[i], data[i + 1], data[i + 2], data[i + 3] });
                }
            }
        }
    }
    internal class Animation
    {
        public float AnimationTime = 0.0f;
        private int frameIndex = 0;
        private int frameIndexNext = 1;
        private float animationTimeDelta = 0.0f;

        public float AnimationDuration = 10.0f;

        private int keyframeAccessorIndex = -1;
        public float[] Keyframes;

        public Dictionary<int, AnimationData> NodeAnimationData = new();

        public Animation(SceneWrapper sceneWrapper, int animationIndex)
        {
            LoadAnimationData(sceneWrapper, animationIndex);
        }

        public float[] GetTranslation(int nodeIndex, string animType)
        {
            int dataSize = 3;
            if (animType == "rotation") dataSize = 4;

            float[] data = new float[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                float outputStart = 0.0f;
                float outputEnd = 0.0f;
                if (animType == "translation")
                {
                    if (NodeAnimationData[nodeIndex].Translations.Count == 0) continue;
                    outputStart = NodeAnimationData[nodeIndex].Translations[frameIndex][i];
                    outputEnd = NodeAnimationData[nodeIndex].Translations[frameIndexNext][i];
                }
                if (animType == "rotation")
                {
                    if (NodeAnimationData[nodeIndex].Rotations.Count == 0) continue;
                    outputStart = NodeAnimationData[nodeIndex].Rotations[frameIndex][i];
                    outputEnd = NodeAnimationData[nodeIndex].Rotations[frameIndexNext][i];
                }
                if (animType == "scale")
                {
                    if (NodeAnimationData[nodeIndex].Scales.Count == 0) continue;
                    outputStart = NodeAnimationData[nodeIndex].Scales[frameIndex][i];
                    outputEnd = NodeAnimationData[nodeIndex].Scales[frameIndexNext][i];
                }

                var outputDelta = outputEnd - outputStart;

                data[i] = outputStart + (animationTimeDelta * outputDelta);
            }
            return data;
        }

        public void UpdateTime(float fps)
        {
            AnimationTime += 1/fps;
            if (AnimationTime > AnimationDuration)
            {
                float excessTime = AnimationTime - AnimationDuration;

                AnimationTime = excessTime % AnimationDuration;
            }

            frameIndex = (int)MathF.Floor(AnimationTime);
            frameIndexNext = (int)MathF.Ceiling(AnimationTime);
            animationTimeDelta = AnimationTime - frameIndex;

            if (frameIndexNext >= AnimationDuration)
            {
                frameIndexNext = 0;
            }
        }

        public void LoadAnimationData(SceneWrapper sceneWrapper, int animationIndex)
        {
            var animation = sceneWrapper.Data.Animations[animationIndex];
            foreach (var channel in animation.Channels)
            {

                var inputAccessorIndex = animation.Samplers[channel.Sampler].Input;
                var outputAccessorIndex = animation.Samplers[channel.Sampler].Output;

                if (keyframeAccessorIndex == -1)
                {
                    var inputBufferViewIndex = sceneWrapper.Data.Accessors[inputAccessorIndex].BufferView.Value;
                    var inputBufferView = sceneWrapper.Data.BufferViews[inputBufferViewIndex];
                    var inputBuffer = sceneWrapper.Data.Buffers[inputBufferView.Buffer];

                    keyframeAccessorIndex = inputAccessorIndex;
                    Keyframes = DataStore.GetFloats(sceneWrapper, inputBuffer, inputBufferView);

                    // right now assuming 1 keyframe = 1 second
                    // should multiply this by a animation speed
                    AnimationDuration = Keyframes.Length;
                }

                // it is expected that all channels within an animation use the same keyframes
                if (inputAccessorIndex != keyframeAccessorIndex)
                {
                    throw new Exception("multiple keyframes found for this animation");
                }

                var targetNodeIndex = channel.Target.Node.Value;
                if (!NodeAnimationData.ContainsKey(targetNodeIndex))
                {
                    NodeAnimationData.Add(targetNodeIndex, new());
                }

                var outputBufferViewIndex = sceneWrapper.Data.Accessors[outputAccessorIndex].BufferView.Value;
                var outputBufferView = sceneWrapper.Data.BufferViews[outputBufferViewIndex];
                var outputBuffer = sceneWrapper.Data.Buffers[outputBufferView.Buffer];
                var outputData = DataStore.GetFloats(sceneWrapper, outputBuffer, outputBufferView);

                NodeAnimationData[targetNodeIndex].AddData(channel.Target.Path, outputData);
            }
        }
    }
}
