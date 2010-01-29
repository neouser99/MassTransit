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
namespace MassTransit.Context
{
	using System;

	/// <summary>
	/// The base message context, including all the message headers
	/// </summary>
	public interface IMessageContext
	{
		/// <summary>
		/// The address to which the message was originally sent
		/// </summary>
		Uri DestinationAddress { get; }

		/// <summary>
		/// The address where responses to this message should be sent
		/// </summary>
		Uri ResponseAddress { get; }

		/// <summary>
		/// The address where faults generated by consumers of this message should be sent
		/// </summary>
		Uri FaultAddress { get; }

		/// <summary>
		/// The address from which this message originated
		/// </summary>
		Uri SourceAddress { get; }

		/// <summary>
		/// The number of times this message has been delivered to the consumer
		/// </summary>
		int RetryCount { get; }

		/// <summary>
		/// The expiration time of the message, if set, otherwise null
		/// </summary>
		DateTime? ExpirationTime { get; }

		/// <summary>
		/// The type of the message in FullName, Assembly format
		/// </summary>
		string MessageType { get; }
	}
}