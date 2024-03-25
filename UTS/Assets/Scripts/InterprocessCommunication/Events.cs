using System;

public delegate void WaitingForClient(IInterprocessCommunication sender);
public delegate void ConnectedToClient(IInterprocessCommunication sender);
public delegate void Disconnected(IInterprocessCommunication sender, Exception? e);
public delegate void ReceiveMessage(IInterprocessCommunication sender, byte[] bytes);
public delegate void Logging(IInterprocessCommunication sender, string msg);
public delegate void FailSendMessage(IInterprocessCommunication sender, Exception? exception);
public delegate void QueueOverflow<T>(T sender);