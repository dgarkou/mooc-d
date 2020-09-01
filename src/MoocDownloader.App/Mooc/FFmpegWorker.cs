﻿using MoocDownloader.App.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MoocDownloader.App.Mooc
{
    /// <summary>
    /// FFMPEG worker.
    /// </summary>
    public class FFmpegWorker
    {
        private const string FFMPEG_EXE = "";

        private readonly ConcurrentQueue<CourseVideoInfo> _videoQueue;
        private readonly EventWaitHandle                  _handle;

        private bool _running;

        private static readonly Lazy<FFmpegWorker> _lazy = new Lazy<FFmpegWorker>(() => new FFmpegWorker());

        /// <summary>
        /// FFmpegWorker instance.
        /// </summary>
        public static FFmpegWorker Instance { get; } = _lazy.Value;

        private FFmpegWorker()
        {
            _videoQueue = new ConcurrentQueue<CourseVideoInfo>();
            _handle     = new AutoResetEvent(false);
        }

        public void Enqueue(CourseVideoInfo courseVideoInfo)
        {
            _videoQueue.Enqueue(courseVideoInfo);
            _handle.Set();
        }

        private void Work()
        {
            _running = true;

            while (_running)
            {
                if (_videoQueue.IsEmpty)
                {
                    _handle.WaitOne();
                }

                if (_videoQueue.TryDequeue(out var info))
                {
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            // merge ts files.
                            var args = $@"-f concat -safe 0 -i {info.MergeListFile} -c copy -y {info.VideoFileName}";

                            var cmdWaiter = new AutoResetEvent(false);

                            var process = new Process
                            {
                                StartInfo =
                                {
                                    FileName               = FFMPEG_EXE, // command  
                                    Arguments              = args,       // arguments  
                                    CreateNoWindow         = true,
                                    UseShellExecute        = false, // do not create a window.  
                                    RedirectStandardInput  = true,  // redirect input.  
                                    RedirectStandardOutput = true,  // redirect output.  
                                    RedirectStandardError  = true   // redirect error.  
                                },
                                EnableRaisingEvents = true
                            };

                            process.Exited += (sender, eventArgs) => { cmdWaiter.Set(); };

                            process.Start();
                            cmdWaiter.WaitOne();
                        }
                        catch
                        {
                            _videoQueue.Enqueue(info); // enqueue video info when merge occur exception.
                        }
                    }, TaskCreationOptions.LongRunning);
                }
            }
        }

        /// <summary>
        /// stop download course video.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _handle.Set();
        }


        /// <summary>
        /// start download course video by ffmpeg.
        /// </summary>
        public void Start()
        {
            Task.Factory.StartNew(Work, TaskCreationOptions.LongRunning);
        }
    }
}