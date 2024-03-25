// using System;
//
// namespace WpfApp1;
//
// public class ClientRequestHandler
// {
//     private ViewModelVisualServer _viewModelVisualServer;
//     private InterprocessCommunicationWithTypes _interprocessCommunication = new(new("NuelValenRobotik", true));
//
//
//
//     public ClientRequestHandler(ViewModelVisualServer viewModelVisualServer) {
//         _viewModelVisualServer = viewModelVisualServer;
//
//         _interprocessCommunication.onLog += (_, msg) => Console.WriteLine(msg);
//         _interprocessCommunication.onReceiveMessage += messageReceived;
//         _interprocessCommunication.onWaitingForClient += (_) => _viewModelVisualServer.setStatusToWaitingForClient();
//         _interprocessCommunication.onDisconnected += (_, e) => _viewModelVisualServer.setStatusToDisconnected();
//         _interprocessCommunication.onConnectedToClient += (_) => _viewModelVisualServer.setStatusToClientConnected();
//     }
//
//     public void startListeningAsync() {
//         _interprocessCommunication.startListeningAsync();
//     }
//
//     private void messageReceived(InterprocessCommunicationWithTypes _) {
//         var data = _interprocessCommunication.readMessage<byte[]>();
//         _viewModelVisualServer.processImage(data);
//     }
//
// }