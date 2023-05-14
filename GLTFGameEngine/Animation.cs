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
        public Vector3[] Translations;
        public Quaternion[] Rotations;
        public Vector3[] Scales;
    }
    internal class Animation
    {
        public float AnimationTime = 0.0f;
        public float AnimationDuration = 10.0f;

        private int keyframeAccessorIndex = -1;
        public float[] Keyframes;

        public Dictionary<int, AnimationData> OutputData = new Dictionary<int, AnimationData>();

        public void UpdateTime(float deltaTime)
        {
            AnimationTime += deltaTime;

            if (AnimationTime > AnimationDuration)
            {
                float excessTime = AnimationTime - AnimationDuration;

                AnimationTime = excessTime % AnimationDuration;
            }
        }

        public void LoadAnimationData(SceneWrapper sceneWrapper, int animationIndex)
        {
            var renderAnimation = this;

            var animation = sceneWrapper.Data.Animations[animationIndex];
            
            foreach (var channel in animation.Channels)
            {
                var samplerIndex = channel.Sampler;
                var inputAccessorIndex = animation.Samplers[channel.Sampler].Input;
                var outputAccessorIndex = animation.Samplers[channel.Sampler].Output;

                if (keyframeAccessorIndex == -1)
                {
                    var inputBufferViewIndex = sceneWrapper.Data.Accessors[inputAccessorIndex].BufferView.Value;
                    var inputBufferView = sceneWrapper.Data.BufferViews[inputBufferViewIndex];
                    var inputBuffer = sceneWrapper.Data.Buffers[inputBufferView.Buffer];

                    keyframeAccessorIndex = inputAccessorIndex;
                    Keyframes = DataStore.GetFloats(sceneWrapper, inputBuffer, inputBufferView);
                }

                // it is expected that all channels within an animation use the same keyframes
                if (inputAccessorIndex == keyframeAccessorIndex)
                {
                    throw new Exception("multiple keyframes found for this animation");
                }

                var outputBufferViewIndex = sceneWrapper.Data.Accessors[outputAccessorIndex].BufferView.Value;
                var outputBufferView = sceneWrapper.Data.BufferViews[outputBufferViewIndex];
                var outputBuffer = sceneWrapper.Data.Buffers[outputBufferView.Buffer];
                float[] outputData = DataStore.GetFloats(sceneWrapper, outputBuffer, outputBufferView);


            }
        }
    }
}
