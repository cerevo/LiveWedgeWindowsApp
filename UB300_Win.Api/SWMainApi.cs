using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace Cerevo.UB300_Win.Api {
    public class DiscoverResult {
        public SwApiFindSwAck FindSwAck { get; set; }
        public string DisplayNameString { get; set; }
        public IPAddress Address { get; set; }
    }

    public static class SwMainApi {
        /// <summary>
        ///     Gets the unicast addresses assigned to the network interfaces on the local computer.
        /// </summary>
        /// <returns>List of <see cref="IPAddress"/>.</returns>
        public static IEnumerable<IPAddress> GetAllLocalIPv4Addresses() {
            return NetworkInterface.GetAllNetworkInterfaces()
                                   .Where(i => i.Supports(NetworkInterfaceComponent.IPv4) && i.SupportsMulticast && i.OperationalStatus == OperationalStatus.Up)
                                   .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                                   .Where(uni => uni.Address.AddressFamily == AddressFamily.InterNetwork)
                                   .Select(uni => uni.Address);
        }

        /// <summary>
        ///     Discovers devices on all network interfaces.
        /// </summary>
        /// <returns>An observable sequence containing device information.</returns>
        public static IObservable<DiscoverResult> Discover() {
            var remoteEp = new IPEndPoint(IPAddress.Parse(InternalConfiguration.DiscoveryIp), InternalConfiguration.DiscoveryPort);
            return GetAllLocalIPv4Addresses().Select(uni => DiscoverOnMulticast(remoteEp, uni)).Merge();
        }

        /// <summary>
        ///     Discovers device on specific IP address.
        /// </summary>
        /// <param name="address">Remote IP address.</param>
        /// <returns>An observable sequence containing device information.</returns>
        public static IObservable<DiscoverResult> DiscoverWithIp(IPAddress address) {
            return DiscoverOnSinglecast(new IPEndPoint(address, InternalConfiguration.DiscoveryPort));
        }

        /// <summary>
        ///     Discovers devices on specific network interface.
        /// </summary>
        /// <param name="remoteEp">Multicast address and port.</param>
        /// <param name="localAddress">Local IP address.</param>
        /// <returns>An observable sequence containing device information.</returns>
        private static IObservable<DiscoverResult> DiscoverOnMulticast(IPEndPoint remoteEp, IPAddress localAddress) => Observable.Create<DiscoverResult>(async (observer, cancelToken) => {
            var disposables = new CompositeDisposable();
            var udpClient = new UdpClient(new IPEndPoint(localAddress, remoteEp.Port));
            disposables.Add(udpClient);
            try {
                udpClient.MulticastLoopback = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.JoinMulticastGroup(remoteEp.Address, localAddress);

                // send SW_ID_FindSw
                var cmd = new SwApiCommand { Cmd = SwApiId.FindSw }.ToBytes();
                await udpClient.SendAsync(cmd, cmd.Length, remoteEp);

                // receive SW_ID_FindSwAck
                while(true) {
                    if(cancelToken.IsCancellationRequested) {
                        return;
                    }
                    var timeout = new CancellationTokenSource(InternalConfiguration.NetworkTimeoutMsec);
                    disposables.Add(timeout);
                    var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancelToken);
                    disposables.Add(linkedCancel);
                    try {
                        var received = await udpClient.ReceiveAsync().WithCancellation(linkedCancel.Token);
                        var ack = ParseFindSwAck(received.Buffer, received.RemoteEndPoint);
                        if(ack != null) {
                            // found device
                            observer.OnNext(ack);
                        }
                    } catch(OperationCanceledException) {
                        // no more results.
                        break;
                    }
                }
                observer.OnCompleted();
            } catch(Exception ex) {
                observer.OnError(ex);
            } finally {
                udpClient.DropMulticastGroup(remoteEp.Address);
                disposables.Dispose();
            }
        });

        /// <summary>
        ///     Discovers device on specific IP address and port.
        /// </summary>
        /// <param name="remoteEp">Remote IP address and port.</param>
        /// <returns>An observable sequence containing device information.</returns>
        private static IObservable<DiscoverResult> DiscoverOnSinglecast(IPEndPoint remoteEp) => Observable.Create<DiscoverResult>(async (observer, cancelToken) => {
            var disposables = new CompositeDisposable();
            var udpClient = new UdpClient();
            disposables.Add(udpClient);
            try {
                udpClient.Connect(remoteEp);

                UdpReceiveResult received;
                try {
                    // send SW_ID_FindSw
                    var timeout = new CancellationTokenSource(InternalConfiguration.NetworkTimeoutMsec);
                    disposables.Add(timeout);
                    var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancelToken);
                    disposables.Add(linkedCancel);
                    var cmd = new SwApiCommand { Cmd = SwApiId.FindSw }.ToBytes();
                    if(await udpClient.SendAsync(cmd, cmd.Length).WithCancellation(linkedCancel.Token) != 4) {
                        // cannot send.
                        observer.OnCompleted();
                        return;
                    }
                    // receive SW_ID_FindSwAck
                    timeout = new CancellationTokenSource(InternalConfiguration.NetworkTimeoutMsec);
                    disposables.Add(timeout);
                    linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancelToken);
                    disposables.Add(linkedCancel);
                    received = await udpClient.ReceiveAsync().WithCancellation(linkedCancel.Token);
                } catch(OperationCanceledException) {
                    // no results.
                    observer.OnCompleted();
                    return;
                }

                var ack = ParseFindSwAck(received.Buffer, received.RemoteEndPoint);
                if(ack != null) {
                    // found device
                    observer.OnNext(ack);
                }
                observer.OnCompleted();
            } catch(Exception ex) {
                observer.OnError(ex);
            } finally {
                disposables.Dispose();
            }
        });

        private static DiscoverResult ParseFindSwAck(byte[] buf, IPEndPoint remoteEp) {
            if(SwApiCommand.GetApiId(buf) != SwApiId.FindSwAck) return null;
            var ack = SwApiCommand.FromBytes<SwApiFindSwAck>(buf);
            if(ack == null) return null;

            var result = new DiscoverResult {
                FindSwAck = ack,
                DisplayNameString = Encoding.UTF8.GetString(ack.DisplayName).TrimEnd('\0'),
                Address = remoteEp.Address
            };
            Debug.WriteLine($"Device found. Name='{result.DisplayNameString}' IP={result.Address} PreviewPort={result.FindSwAck.Preview} ControlPort={result.FindSwAck.Command} GeneralPort={result.FindSwAck.Tcp}");
            return result;
        }
    }
}
