﻿using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.IO;
using System.Linq;
using System.Net;

namespace LowLevelDesign.WinTrace.Handlers
{
    class NetworkTraceEventHandler : ITraceEventHandler
    {
        class NetworkIoSummary
        {
            public long Recv;

            public long Send;

            public long Total;
        }

        private readonly TextWriter output;
        private readonly int pid;
        private readonly Dictionary<string, NetworkIoSummary> networkIoSummary = new Dictionary<string, NetworkIoSummary>();

        public NetworkTraceEventHandler(int pid, TextWriter output)
        {
            this.output = output;
            this.pid = pid;

        }

        public void SubscribeToEvents(KernelTraceEventParser kernel)
        {
            kernel.TcpIpAccept += HandleTcpIpConnect;
            kernel.TcpIpAcceptIPV6 += HandleTcpIpV6Connect;
            kernel.TcpIpARPCopy += HandleTcpIp;
            kernel.TcpIpConnect += HandleTcpIpConnect;
            kernel.TcpIpConnectIPV6 += HandleTcpIpV6Connect;
            kernel.TcpIpDisconnect += HandleTcpIp;
            kernel.TcpIpDisconnectIPV6 += HandleTcpIpV6;
            kernel.TcpIpDupACK += HandleTcpIp;
            kernel.TcpIpFail += HandleTcpIpFail;
            kernel.TcpIpFullACK += HandleTcpIp;
            kernel.TcpIpPartACK += HandleTcpIp;
            kernel.TcpIpReconnect += HandleTcpIp;
            kernel.TcpIpReconnectIPV6 += HandleTcpIpV6;
            kernel.TcpIpRecv += HandleTcpIpRev;
            kernel.TcpIpRecvIPV6 += HandleTcpIpV6Rev;
            kernel.TcpIpRetransmit += HandleTcpIp;
            kernel.TcpIpRetransmitIPV6 += HandleTcpIpV6;
            kernel.TcpIpSend += HandleTcpIpSend;
            kernel.TcpIpSendIPV6 += HandleTcpIpV6Send;
            kernel.TcpIpTCPCopy += HandleTcpIp;
            kernel.TcpIpTCPCopyIPV6 += HandleTcpIpV6;
        }

        public void PrintStatistics()
        {
            if (networkIoSummary.Count == 0) {
                return;
            }
            output.WriteLine("======= TCP/IP =======");
            output.WriteLine("Source -> Destination  Send / Receive (bytes)");
            foreach (var summary in networkIoSummary.OrderByDescending(kv => kv.Value.Total)) {
                output.WriteLine($"{summary.Key} {summary.Value.Send:#,0} / {summary.Value.Recv:#,0}");
            }
        }

        private void HandleTcpIpConnect(TcpIpConnectTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
            }
        }

        private void HandleTcpIpV6Connect(TcpIpV6ConnectTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
            }
        }

        private void HandleTcpIp(TcpIpTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
            }
        }

        private void HandleTcpIpV6(TcpIpV6TraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
            }
        }

        private void HandleTcpIpRev(TcpIpTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.daddr}:{data.dport} <- {data.saddr}:{data.sport} (0x{data.connid:X})");
                UpdateStats(data.saddr, data.daddr, true, data.size);
            }
        }

        private void HandleTcpIpV6Rev(TcpIpV6TraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.daddr}:{data.dport} <- {data.saddr}:{data.sport} (0x{data.connid:X})");
                UpdateStats(data.saddr, data.daddr, true, data.size);
            }
        }

        private void HandleTcpIpFail(TcpIpFailTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"0x{data.FailureCode:X}");
            }
        }

        private void HandleTcpIpSend(TcpIpSendTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
                UpdateStats(data.saddr, data.daddr, false, data.size);
            }
        }

        private void HandleTcpIpV6Send(TcpIpV6SendTraceData data)
        {
            if (data.ProcessID == pid) {
                output.WriteLine($"{data.TimeStampRelativeMSec:0.0000} {data.EventName} " + 
                    $"{data.saddr}:{data.sport} -> {data.daddr}:{data.dport} (0x{data.connid:X})");
                UpdateStats(data.saddr, data.daddr, false, data.size);
            }
        }

        private void UpdateStats(IPAddress saddr, IPAddress daddr, bool isReceive, int size)
        {
            string key = $"{saddr} -> {daddr}";
            NetworkIoSummary summary;
            if (!networkIoSummary.TryGetValue(key, out summary)) {
                summary = new NetworkIoSummary();
                networkIoSummary.Add(key, summary);
            }
            if (isReceive) {
                summary.Recv += size;
                summary.Total += size;
            } else { 
                summary.Send += size;
                summary.Total += size;
            }
        }
    }
}
