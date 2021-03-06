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
namespace MassTransit.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

	/// <summary>
	/// A saga repository is used by the service bus to dispatch messages to sagas
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public interface ISagaRepository<T> :
		IDisposable
        where T : class, ISaga
    {
		void Send<TMessage>(Expression<Func<T, bool>> filter, ISagaPolicy<T, TMessage> policy, TMessage message, Action<T> consumerAction)
			where TMessage : class;

		IEnumerable<T> Where(Expression<Func<T, bool>> filter);
    }
}