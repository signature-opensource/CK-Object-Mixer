using CK.Core;
using CK.Object.Processor.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace CK.Object.Processor
{
    public sealed class AddRandomFriendToUserProcessorConfiguration : ObjectProcessorConfiguration
    {
        readonly int _minAge;

        public AddRandomFriendToUserProcessorConfiguration( IActivityMonitor monitor,
                                                            TypedConfigurationBuilder builder,
                                                            ImmutableConfigurationSection configuration,
                                                            IReadOnlyList<ObjectProcessorConfiguration> processors )
            : base( monitor, builder, configuration, processors )
        {
            _minAge = configuration.TryGetIntValue( monitor, "MinAge", 1, 99 ) ?? 0;
            SetIntrinsicCondition( Condition );
            SetIntrinsicTransform( Transform );
        }

        Func<object, bool>? Condition( IServiceProvider services )
        {
            return o => o is UserRecord u && u.Age >= _minAge;
        }

        Func<object, object>? Transform( IServiceProvider services )
        {
            var userServices = services.GetRequiredService<UserService>();
            return o =>
            {
                var u = ((UserRecord)o);
                u.Friends.Add( userServices.Users[Random.Shared.Next( userServices.Users.Count )] );
                return o;
            };
        }

    }
}
