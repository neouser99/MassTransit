// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.Msmq
{
	using System;
	using System.Diagnostics;
	using System.Messaging;
	using System.Runtime.Serialization;
	using Configuration;
	using Context;
	using Internal;
	using log4net;
	using Serialization;

    [DebuggerDisplay("{Address}")]
    public class MsmqEndpoint :
		AbstractEndpoint
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (MsmqEndpoint));

    	private readonly MessageRetryTracker _tracker;
		private bool _disposed;
		private IMsmqTransport _errorTransport;
		private IMsmqTransport _transport;

		public MsmqEndpoint(IMsmqEndpointAddress address, IMessageSerializer serializer, IMsmqTransport transport, IMsmqTransport errorTransport)
			: base(address, serializer)
		{
			_tracker = new MessageRetryTracker(5);

			_transport = transport;
			_errorTransport = errorTransport;

			SetDisposedMessage();
		}

		public override void Send<T>(T message)
		{
			Send(message, new PublishContext());
		}

    	public override void Send<T>(T message, ISendContext context)
		{
			if (_disposed) throw NewDisposedException();

    		_transport.Send(msg =>
    			{
    				SetOutboundMessageHeaders<T>(context);

    				PopulateTransportMessage(msg, message, context);
    			}, context);
		}

		public override void Receive(Func<object, Action<object>> receiver, IReceiveContext context)
		{
			if (_disposed) throw NewDisposedException();

			_transport.Receive(ReceiveFromTransport(receiver, context), context);
		}

		public override void Receive(Func<object, Action<object>> receiver, IReceiveContext context, TimeSpan timeout)
		{
			if (_disposed) throw NewDisposedException();

			_transport.Receive(ReceiveFromTransport(receiver, context), context, timeout);
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				_transport.Dispose();
				_transport = null;

				_errorTransport.Dispose();
				_errorTransport = null;

				base.Dispose(true);
			}

			_disposed = true;
		}

		private void PopulateTransportMessage<T>(Message transportMessage, T message, ISendContext context)
		{
			Serializer.Serialize(transportMessage.BodyStream, message, context);

			transportMessage.Label = typeof (T).Name;

			transportMessage.Recoverable = true;
		}

		private Func<Message, Action<Message>> ReceiveFromTransport(Func<object, Action<object>> receiver, IReceiveContext context)
		{
			return message =>
				{
					if (_tracker.IsRetryLimitExceeded(message.Id))
					{
						if(_log.IsErrorEnabled)
							_log.ErrorFormat("Message retry limit exceeded {0}:{1}", Address, message.Id);

						return x => MoveMessageToErrorTransport(x, context);
					}

					object messageObj;

					try
					{
						messageObj = Serializer.Deserialize(message.BodyStream, context);
					}
					catch (SerializationException sex)
					{
                        if (_log.IsErrorEnabled)
                            _log.Error("Unrecognized message " + Address + ":" + message.Id, sex);

						_tracker.IncrementRetryCount(message.Id);
						return x => MoveMessageToErrorTransport(x, context);
					}

					if (messageObj == null)
						return null;

					Action<object> receive;
					try
					{
						receive = receiver(messageObj);
						if (receive == null)
						{
							if (_log.IsDebugEnabled)
								_log.DebugFormat("SKIP:{0}:{1}", Address, messageObj.GetType().Name);

							if (SpecialLoggers.Messages.IsInfoEnabled)
								SpecialLoggers.Messages.InfoFormat("SKIP:{0}:{1}", Address, messageObj.GetType().Name);

							return null;
						}
					}
					catch (Exception ex)
					{
						if (_log.IsErrorEnabled)
							_log.Error("An exception was thrown preparing the message consumers", ex);

						_tracker.IncrementRetryCount(message.Id);
						return null;
					}

					return m =>
					    {
                            if (_log.IsDebugEnabled)
                                _log.DebugFormat("RECV:{0}:{1}:{2}", Address, m.Id, messageObj.GetType().Name);

                            if (SpecialLoggers.Messages.IsInfoEnabled)
                                SpecialLoggers.Messages.InfoFormat("RECV:{0}:{1}:{2}", Address, m.Id, messageObj.GetType().Name);

					        try
					        {
					            receive(messageObj);

					        	_tracker.MessageWasReceivedSuccessfully(message.Id);
					        }
					        catch (Exception ex)
					        {
                                if(_log.IsErrorEnabled)
									_log.Error("An exception was thrown by a message consumer", ex);

								_tracker.IncrementRetryCount(message.Id);
								MoveMessageToErrorTransport(m, context);
							}
					    };
				};
		}

	    private void MoveMessageToErrorTransport(Message message, IMessageContext context)
	    {
	    	_errorTransport.Send(outbound =>
	    		{
	    			outbound.BodyStream = message.BodyStream;

	    			SetMessageExpiration(outbound, context);
	    		}, context);

	        if (_log.IsDebugEnabled)
	            _log.DebugFormat("MOVE:{0}:{1}:{2}", Address, _errorTransport.Address, message.Id);

	        if (SpecialLoggers.Messages.IsInfoEnabled)
	            SpecialLoggers.Messages.InfoFormat("MOVE:{0}:{1}:{2}", Address, _errorTransport.Address, message.Id);
	    }

    	private static void SetMessageExpiration(Message outbound, IMessageContext context)
    	{
			if (context.ExpirationTime.HasValue)
    		{
    			outbound.TimeToBeReceived = context.ExpirationTime.Value - DateTime.UtcNow;
    		}
    	}

    	public static IEndpoint ConfigureEndpoint(Uri uri, Action<IEndpointConfigurator> configurator)
		{
			if (uri.Scheme.ToLowerInvariant() == "msmq")
			{
				IEndpoint endpoint = MsmqEndpointConfigurator.New(x =>
					{
						x.SetUri(uri);

						configurator(x);
					});

				return endpoint;
			}

			return null;
		}
	}
}