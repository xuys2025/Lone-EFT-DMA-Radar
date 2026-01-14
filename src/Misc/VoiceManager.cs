using System.Collections.Concurrent;
using System.IO;
using System.Media;

namespace LoneEftDmaRadar.Misc
{
    public static class VoiceManager
    {
        private static readonly ConcurrentQueue<string> _playbackQueue = new();
        private static readonly SoundPlayer _player = new();
        private static CancellationTokenSource _cts;
        private static readonly object _lock = new();
        private static string _currentFile = null;

        /// <summary>
        /// Directory containing voice files.
        /// </summary>
        public static string VoiceDir { get; } = Path.Combine(Environment.CurrentDirectory, "Resources", "voice");

        /// <summary>
        /// Starts the background playback thread.
        /// </summary>
        public static void Start()
        {
            lock (_lock)
            {
                if (_cts != null) return;
                _cts = new CancellationTokenSource();
                new Thread(() => PlaybackLoop(_cts.Token))
                {
                    IsBackground = true,
                    Name = "VoiceManager Thread"
                }.Start();
            }
        }

        /// <summary>
        /// Stops the background playback thread.
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = null;
                _player.Stop();
                _currentFile = null;
                _playbackQueue.Clear();
            }
        }

        /// <summary>
        /// Queues a voice file for playback.
        /// </summary>
        /// <param name="fileName">Filename without extension (e.g. "RAID_START")</param>
        /// <param name="interrupt">If true, stops current playback and clears queue before playing this.</param>
        public static void Play(string fileName, bool interrupt = false)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            // Simple check if voice is enabled in config (to be implemented in Config)
            if (!Program.Config.Voice.Enabled) return;

            if (interrupt)
            {
                StopPlayback();
                _playbackQueue.Clear(); // Clear existing queue
                _playbackQueue.Enqueue(fileName);
            }
            else
            {
                _playbackQueue.Enqueue(fileName);
            }
        }

        private static void StopPlayback()
        {
            try
            {
                _player.Stop();
            }
            catch { }
        }

        private static void PlaybackLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_playbackQueue.TryDequeue(out var fileName))
                {
                    var fullPath = Path.Combine(VoiceDir, $"{fileName}.wav");
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            _currentFile = fileName;
                            _player.SoundLocation = fullPath;
                            _player.Load(); // Load synchronously to ensure it's ready
                            _player.PlaySync(); // Play synchronously so we don't skip to next
                        }
                        catch (Exception ex)
                        {
                            Logging.WriteLine($"[VoiceManager] Error playing '{fileName}': {ex.Message}");
                        }
                        finally
                        {
                            _currentFile = null;
                        }
                    }
                    else
                    {
                        Logging.WriteLine($"[VoiceManager] File not found: {fullPath}");
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
