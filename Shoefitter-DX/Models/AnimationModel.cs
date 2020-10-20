using SAGESharp.Animations;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace ShoefitterDX.Models
{
    public class AnimationQuaternionKeyframeModel : INotifyPropertyChanged
    {
        private float _time;
        public float Time
        {
            get => this._time;
            set
            {
                this._time = value;
                RaisePropertyChanged(nameof(Time));
            }
        }

        private Quaternion _value;
        public Quaternion Value
        {
            get => this._value;
            set
            {
                this._value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        public AnimationQuaternionKeyframeModel(float time, Quaternion value)
        {
            this.Time = time;
            this.Value = value;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class AnimationVectorKeyframeModel : INotifyPropertyChanged
    {
        private float _time;
        public float Time
        {
            get => this._time;
            set
            {
                this._time = value;
                RaisePropertyChanged(nameof(Time));
            }
        }

        private float _valueX;
        public float ValueX
        {
            get => this._valueX;
            set
            {
                this._valueX = value;
                RaisePropertyChanged(nameof(ValueX));
            }
        }

        private float _valueY;
        public float ValueY
        {
            get => this._valueY;
            set
            {
                this._valueY = value;
                RaisePropertyChanged(nameof(ValueY));
            }
        }

        private float _valueZ;
        public float ValueZ
        {
            get => this._valueZ;
            set
            {
                this._valueZ = value;
                RaisePropertyChanged(nameof(ValueZ));
            }
        }

        public Vector3 Value => new Vector3(ValueX, ValueY, ValueZ);

        public AnimationVectorKeyframeModel(float time, Vector3 value)
        {
            this.Time = time;
            this.ValueX = value.X;
            this.ValueY = value.Y;
            this.ValueZ = value.Z;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class AnimationQuaternionChannelModel
    {
        public ObservableCollection<AnimationQuaternionKeyframeModel> Keyframes { get; } = new ObservableCollection<AnimationQuaternionKeyframeModel>();

        public string Name { get; }

        public AnimationQuaternionChannelModel(string name)
        {
            this.Name = name;
        }

        public int IndexOfKeyframeAtOrBefore(float time)
        {
            if (Keyframes.Count == 0)
                return -1;

            // If the first keyframe is *already* past the target time, there is no keyframe before.
            if (Keyframes[0].Time > time)
                return -1;

            for (int i = 0; i < Keyframes.Count - 1; i++)
            {
                if (Keyframes[i + 1].Time > time)
                {
                    return i;
                }
            }

            // If no keyframes were past, return the last keyframe
            return Keyframes.Count - 1;
        }

        public Quaternion? Evaluate(float time)
        {
            if (Keyframes.Count == 0)
                return null;

            int beforeIndex = IndexOfKeyframeAtOrBefore(time);
            if (beforeIndex == -1)
            {
                // Pre-Infinity: Constant
                return Keyframes[0].Value;
            }
            else if (beforeIndex == Keyframes.Count - 1)
            {
                // Post-Infinity: Constant
                return Keyframes[Keyframes.Count - 1].Value;
            }
            else
            {
                // Interpolate
                AnimationQuaternionKeyframeModel beforeKeyframe = Keyframes[beforeIndex];
                AnimationQuaternionKeyframeModel afterKeyframe = Keyframes[beforeIndex + 1];
                return Quaternion.Slerp(beforeKeyframe.Value, afterKeyframe.Value, (time - beforeKeyframe.Time) / (afterKeyframe.Time - beforeKeyframe.Time));
            }
        }
    }

    public class AnimationVectorChannelModel
    {
        public ObservableCollection<AnimationVectorKeyframeModel> Keyframes { get; } = new ObservableCollection<AnimationVectorKeyframeModel>();

        public string Name { get; }

        public AnimationVectorChannelModel(string name)
        {
            this.Name = name;
        }

        public int IndexOfKeyframeAtOrBefore(float time)
        {
            if (Keyframes.Count == 0)
                return -1;

            // If the first keyframe is *already* past the target time, there is no keyframe before.
            if (Keyframes[0].Time > time)
                return -1;
            
            for (int i = 0; i < Keyframes.Count - 1; i++)
            {
                if (Keyframes[i + 1].Time > time)
                {
                    return i;
                }
            }

            // If no keyframes were past, return the last keyframe
            return Keyframes.Count - 1;
        }

        public Vector3? Evaluate(float time)
        {
            if (Keyframes.Count == 0)
                return null;

            int beforeIndex = IndexOfKeyframeAtOrBefore(time);
            if (beforeIndex == -1)
            {
                // Pre-Infinity: Constant
                return Keyframes[0].Value;
            }
            else if (beforeIndex == Keyframes.Count - 1)
            {
                // Post-Infinity: Constant
                return Keyframes[Keyframes.Count - 1].Value;
            }
            else
            {
                // Interpolate
                AnimationVectorKeyframeModel beforeKeyframe = Keyframes[beforeIndex];
                AnimationVectorKeyframeModel afterKeyframe = Keyframes[beforeIndex + 1];
                return Vector3.Lerp(beforeKeyframe.Value, afterKeyframe.Value, (time - beforeKeyframe.Time) / (afterKeyframe.Time - beforeKeyframe.Time));
            }
        }
    }

    public class AnimationTrackModel : INotifyPropertyChanged
    {
        private int _boneID;
        public int BoneID
        {
            get => this._boneID;
            set
            {
                this._boneID = value;
                this.RaisePropertyChanged(nameof(BoneID));
            }
        }

        public AnimationVectorChannelModel TranslationChannel { get; } = new AnimationVectorChannelModel("Translation");
        public AnimationQuaternionChannelModel RotationChannel { get; } = new AnimationQuaternionChannelModel("Rotation");
        public AnimationVectorChannelModel ScaleChannel { get; } = new AnimationVectorChannelModel("Scale");

        public Matrix Evaluate(float time, Matrix defaultPose)
        {
            Vector3 translation = TranslationChannel.Evaluate(time) ?? Vector3.Zero;
            Quaternion rotation = RotationChannel.Evaluate(time) ?? Quaternion.Identity;
            rotation.Invert();
            //Vector3 scale = ScaleChannel.Evaluate(time) ?? Vector3.One;
            return Matrix.Translation(translation) * Matrix.RotationQuaternion(rotation) * defaultPose; // TODO: Implement scale
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public class AnimationModel : INotifyPropertyChanged
    {
        private float _duration;
        public float Duration
        {
            get => this._duration;
            set
            {
                this._duration = value;
                this.RaisePropertyChanged(nameof(Duration));
            }
        }

        private string _filename;
        public string Filename
        {
            get => this._filename;
            set
            {
                this._filename = value;
                RaisePropertyChanged(nameof(Filename));
            }
        }

        public ObservableCollection<AnimationTrackModel> Tracks { get; } = new ObservableCollection<AnimationTrackModel>();

        public AnimationModel(string filename)
        {
            this.Filename = filename;
        }

        public void Evaluate(float time, Matrix[] defaultPose, Matrix[] outputPose)
        {
            for (int i = 0; i < defaultPose.Length; i++)
            {
                outputPose[i] = defaultPose[i];
            }
            foreach (AnimationTrackModel track in this.Tracks)
            {
                outputPose[track.BoneID] = track.Evaluate(time, defaultPose[track.BoneID]);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public static AnimationModel FromBKD(SAGESharp.Animations.BKD bkd, string filename)
        {
            AnimationModel result = new AnimationModel(filename);

            result.Duration = bkd.Length;
            foreach (TransformAnimation transform in bkd.Entries)
            {
                if (transform.BoneID == ushort.MaxValue)
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING]: BKD {filename} has a transform with a -1 Bone ID! Ignoring.");
                    continue;
                }

                //System.Diagnostics.Debug.WriteLine($"Animation for bone {transform.BoneID} + ({SAGESharp.BHDFile.NonBipedBoneNames[transform.BoneID]})");

                AnimationTrackModel track = new AnimationTrackModel();
                track.BoneID = transform.BoneID;

                foreach (VectorKeyframe translationKeyframe in transform.TranslationKeyframes)
                {
                    track.TranslationChannel.Keyframes.Add(new AnimationVectorKeyframeModel(translationKeyframe.Frame / (float)BKD.FRAMES_PER_SECOND, new Vector3(translationKeyframe.X, translationKeyframe.Y, translationKeyframe.Z)));
                }

                foreach (VectorKeyframe scaleKeyframe in transform.ScaleKeyframes)
                {
                    track.ScaleChannel.Keyframes.Add(new AnimationVectorKeyframeModel(scaleKeyframe.Frame / (float)BKD.FRAMES_PER_SECOND, new Vector3(scaleKeyframe.X, scaleKeyframe.Y, scaleKeyframe.Z)));
                }

                foreach (QuaternionKeyframe rotationKeyframe in transform.RotationKeyframes)
                {
                    track.RotationChannel.Keyframes.Add(new AnimationQuaternionKeyframeModel(rotationKeyframe.Frame / (float)BKD.FRAMES_PER_SECOND, new Quaternion(rotationKeyframe.X, rotationKeyframe.Y, rotationKeyframe.Z, rotationKeyframe.W)));
                }
                result.Tracks.Add(track);
            }

            return result;
        }
    }
}
