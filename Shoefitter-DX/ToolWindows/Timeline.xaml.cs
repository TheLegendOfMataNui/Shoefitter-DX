using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace ShoefitterDX.ToolWindows
{
    public class TimelineContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int _startFrame;
        public int StartFrame
        {
            get => this._startFrame;
            set
            {
                this._startFrame = value;
                this.RaisePropertyChanged(nameof(StartFrame));
                this.RaisePropertyChanged(nameof(StartTime));
                this.RaisePropertyChanged(nameof(EndTime));
            }
        }

        private int _frameCount;
        public int FrameCount
        {
            get => this._frameCount;
            set
            {
                this._frameCount = value;
                this.RaisePropertyChanged(nameof(FrameCount));
                this.RaisePropertyChanged(nameof(EndTime));
            }
        }

        private float _frameLength;
        public float FrameLength
        {
            get => this._frameLength;
            set
            {
                this._frameLength = value;
                this.RaisePropertyChanged(nameof(FrameLength));
                this.RaisePropertyChanged(nameof(StartTime));
                this.RaisePropertyChanged(nameof(EndTime));
                this.RaisePropertyChanged(nameof(CurrentTime));
            }
        }

        private int _currentFrame;
        public int CurrentFrame
        {
            get => this._currentFrame;
            set
            {
                this._currentFrame = value;
                this.RaisePropertyChanged(nameof(CurrentFrame));
                this.RaisePropertyChanged(nameof(CurrentTime));
                this.FrameChange?.Invoke(this, this.CurrentFrame);
            }
        }

        private float _timeScale = 1.0f;
        public float TimeScale
        {
            get => this._timeScale;
            set
            {
                this._timeScale = value;
                this.RaisePropertyChanged(nameof(TimeScale));
            }
        }

        private bool _isPaused = true;
        public bool IsPaused
        {
            get => this._isPaused;
            set
            {
                bool wasPaused = this.IsPaused;
                this._isPaused = value;
                this.RaisePropertyChanged(nameof(IsPaused));
                if (this.IsPaused && !wasPaused)
                {
                    this.CancellationSource?.Cancel();
                    this.CancellationSource = null;
                }
                else if (!this.IsPaused && wasPaused)
                {
                    this.CancellationSource = new CancellationTokenSource();
                    SynchronizationContext mainContext = SynchronizationContext.Current;
                    Task.Run(async () =>
                    {
                        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                        stopwatch.Start();
                        float elapsedTime = 0.0f;
                        do
                        {
                            //System.Diagnostics.Debug.WriteLine("Waiting for frame");
                            await Task.Delay((int)(this.FrameLength * 1000));
                            elapsedTime += (float)stopwatch.Elapsed.TotalSeconds * this.TimeScale;
                            stopwatch.Restart();
                            int increment = (int)Math.Floor(elapsedTime / this.FrameLength);
                            elapsedTime -= increment * this.FrameLength;
                            mainContext.Send(state =>
                            {
                                int nextFrame = this.CurrentFrame + increment;
                                while (nextFrame >= this.StartFrame + this.FrameCount)
                                    nextFrame -= this.FrameCount;
                                this.CurrentFrame = nextFrame;
                                //System.Diagnostics.Debug.WriteLine("Updated current frame");
                            }, null);
                        } while (!this.CancellationSource?.IsCancellationRequested ?? false);
                        stopwatch.Stop();
                    });
                }
            }
        }

        private string _label;
        public string Label
        {
            get => this._label;
            set
            {
                this._label = value;
                this.RaisePropertyChanged(nameof(Label));
            }
        }

        public float StartTime => this.StartFrame * this.FrameLength;
        public float EndTime => (this.StartFrame + this.FrameCount) * this.FrameLength;
        public float CurrentTime => this.CurrentFrame * this.FrameLength;

        public event EventHandler<int> FrameChange;

        private CancellationTokenSource CancellationSource;

        public TimelineContext(int startFrame, int frameCount, float frameLength, string label = "")
        {
            this._startFrame = startFrame;
            this._frameCount = frameCount;
            this._frameLength = frameLength;
            this._label = label;
        }

        public void Play()
        {
            this.IsPaused = false;
        }

        public void Pause()
        {
            this.IsPaused = true;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public static DependencyProperty ContextProperty = DependencyProperty.Register(nameof(Context), typeof(TimelineContext), typeof(Timeline));
        public TimelineContext Context
        {
            get => (TimelineContext)this.GetValue(ContextProperty);
            set => this.SetValue(ContextProperty, value);
        }

        public Timeline()
        {
            InitializeComponent();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Context.IsPaused = !this.Context.IsPaused;
        }

        private void SkipToEndButton_Click(object sender, RoutedEventArgs e)
        {
            this.Context.CurrentFrame = this.Context.StartFrame + this.Context.FrameCount - 1;
        }

        private void SkipToBeginningButton_Click(object sender, RoutedEventArgs e)
        {
            this.Context.CurrentFrame = this.Context.StartFrame;
        }
    }
}
