using System.Collections.Generic;

namespace CK.Object.Processor.Tests;

public class UserService
{
    public UserService()
    {
        var a = new UserRecord( "Albert", 65, new List<UserRecord>() );
        var b = new UserRecord( "Bernard", 14, new List<UserRecord>() );
        var c = new UserRecord( "Clark", 41, new List<UserRecord>() );
        var d = new UserRecord( "Deora", 6, new List<UserRecord>() );
        var e = new UserRecord( "Eno", 42, new List<UserRecord>() );
        Users = new[] { a, b, c, d, e };
    }

    public IReadOnlyList<UserRecord> Users { get; }
}
