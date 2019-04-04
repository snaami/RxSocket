﻿using System;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Diagnostics;

#nullable enable

namespace RxSockets.Tests
{
    public class PerformanceTest : IAsyncLifetime
    {
        private readonly IPEndPoint EndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        private IRxSocketServer? server;
        private IRxSocketClient? client, accept;

        private readonly Action<string> Write;
        public PerformanceTest(ITestOutputHelper output) =>  Write = output.WriteLine;

        public async Task InitializeAsync()
        {
            server = RxSocketServer.Create(EndPoint);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            client = await RxSocketClient.ConnectAsync(EndPoint);
            accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);
        }

        public async Task DisposeAsync() =>
            await Task.WhenAll(client?.DisconnectAsync(), accept?.DisconnectAsync(), server?.DisconnectAsync());

        [Fact]
        public async Task T01_ReceiveStrings()
        {
            if (accept == null || client == null)
                throw new Exception("Accept and/or Client is null.");

            var messages = 100_000;

            var message = "Welcome!".ToByteArray();

            var countTask = client.ReceiveObservable.ToStrings().Count().ToTask();

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < messages; i++)
                accept.Send(message);
            await accept.DisconnectAsync();

            var count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");
        }

        [Fact]
        public async Task T02_ReceiveStringsFromPrefixedBytes()
        {
            if (accept == null || client == null)
                throw new Exception("Accept and/or Client is null.");


            var messages = 100_000;

            var message = new [] { "Welcome!" }.ToByteArrayWithLengthPrefix();

            var countTask = client.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().Count().ToTask();

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < messages; i++)
                accept.Send(message);
            await accept.DisconnectAsync();

            int count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");
        }
    }
}
