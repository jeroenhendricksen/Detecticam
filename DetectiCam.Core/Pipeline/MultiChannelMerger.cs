﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DetectiCam.Core.VideoCapturing
{
    public class MultiChannelMerger<T> : IDisposable
    {
        private readonly List<ChannelReader<T>?> _inputReaders;
        private readonly ChannelWriter<IList<T>> _outputWriter;
        private readonly ILogger _logger;
        private Task? _mergeTask = null;
        private CancellationTokenSource _internalCts;
        private bool disposedValue;

        public MultiChannelMerger(IEnumerable<ChannelReader<T>> inputReaders, ChannelWriter<IList<T>> outputWriter,
            ILogger logger)
        {
            _inputReaders = new List<ChannelReader<T>?>(inputReaders);
            _outputWriter = outputWriter;
            _logger = logger;
            _internalCts = new CancellationTokenSource();
        }

        public Task ExecuteProcessingAsync(CancellationToken stoppingToken)
        { 
            _mergeTask = Task.Run(async () =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        _internalCts.Token, stoppingToken);
                    var linkedToken = cts.Token;

                    while (true)
                    {
                        linkedToken.ThrowIfCancellationRequested();
                        List<T> results = new List<T>();

                        for (var index = 0; index < _inputReaders.Count; index++)
                        {
                            try
                            {
                                var curReader = _inputReaders[index];
                                if (curReader != null)
                                {
                                    var result = await curReader.ReadAsync(linkedToken).ConfigureAwait(false);
                                    results.Add(result);
                                }
                            }
                            catch (ChannelClosedException)
                            {
                                _logger.LogWarning("Channel closed");
                                _inputReaders[index] = null;
                            }
                        }
                        if (results.Count > 0)
                        {
                            if (!_outputWriter.TryWrite(results))
                            {
                                _logger.LogWarning("Could not write merged result!");
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _logger.LogInformation("Stopping:completing merge channel!");
                    //Complete the channel since nothing to be read anymore
                    _outputWriter.TryComplete();
                }
            }, stoppingToken);

            return _mergeTask;
        }

        public async Task StopProcessingAsync(CancellationToken cancellationToken)
        {
            _internalCts.Cancel();
            if (_mergeTask != null)
            {
                await _mergeTask;
                _mergeTask = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _internalCts?.Dispose();

                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MultiChannelMerger()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
