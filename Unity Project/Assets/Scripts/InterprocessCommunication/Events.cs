using System;

public delegate void WaitingForClient(IInterprocessCommunication sender);
public delegate void Connected(IInterprocessCommunication sender);
public delegate void EstablishingNetwork(IInterprocessCommunication sender);
public delegate void Disconnected(IInterprocessCommunication sender, Exception? e);
public delegate void ReceiveMessage(IInterprocessCommunication sender, byte[] bytes);
public delegate void Logging(IInterprocessCommunication sender, string msg);
public delegate void StopListening(IInterprocessCommunication sender);
public delegate void FailSendMessage(IInterprocessCommunication sender, Exception? exception, byte[] message);
public delegate void QueueOverflow<T>(T sender);



public delegate void NewMessageNotification(InterprocessCommunicationWithTypes sender);