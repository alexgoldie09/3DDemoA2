using System.Diagnostics;

namespace _3DDemoA2.Debugging
{
    /// <summary>
    /// Tracks the average frame time and CPU render time over a short sample of frames.
    /// 
    /// Frame time is the whole gap between frames, including the VSync wait, so it follows the monitor's refresh rate.
    /// CPU render time is only how long this program spends issuing a frame's work, which is the part code changes affect.
    /// </summary>
    internal class FrameTimer
    {
        #region Constants
        // How long each average covers, in seconds
        private const double SAMPLESECONDS = 0.5;
        #endregion

        #region Public Fields
        /// <summary>
        /// Average time between frames over the last sample, in milliseconds.
        /// </summary>
        public double AverageFrameMs { get; private set; }

        /// <summary>
        /// Average CPU time spent issuing each frame over the last sample, in milliseconds.
        /// </summary>
        public double AverageRenderMs { get; private set; }

        /// <summary>
        /// Average frames per second over the last sample.
        /// </summary>
        public double AverageFps => 1000.0 / Math.Max(AverageFrameMs, 0.001); // the minimum avoids dividing by zero
        #endregion

        #region Private Fields
        // Times the CPU work of one frame
        private readonly Stopwatch _renderStopwatch = new();

        // Running totals for the current sample, turned into averages when it ends
        private double _sampleElapsed;
        private double _sampleFrameTime;
        private double _sampleRenderTime;
        private int _sampleFrames;
        #endregion

        #region Public API
        /// <summary>
        /// Starts timing a frame's work. Call at the start of OnRenderFrame.
        /// </summary>
        public void BeginFrame() => _renderStopwatch.Restart();

        /// <summary>
        /// Stops timing the frame's work and adds this frame to the averages.
        /// Call before SwapBuffers so the wait isn't counted as render time.
        /// </summary>
        /// <param name="frameSeconds">Time since the previous frame, in seconds.</param>
        public void EndFrame(double frameSeconds)
        {
            _renderStopwatch.Stop();

            _sampleElapsed += frameSeconds;
            _sampleFrameTime += frameSeconds * 1000.0;
            _sampleRenderTime += _renderStopwatch.Elapsed.TotalMilliseconds;
            _sampleFrames++;

            if (_sampleElapsed < SAMPLESECONDS)
                return;

            AverageFrameMs = _sampleFrameTime / _sampleFrames;
            AverageRenderMs = _sampleRenderTime / _sampleFrames;

            _sampleElapsed = 0;
            _sampleFrameTime = 0;
            _sampleRenderTime = 0;
            _sampleFrames = 0;
        }
        #endregion
    }
}
