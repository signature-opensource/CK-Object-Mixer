using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Object.Processor.Tests;

public record class UserRecord( string Name, int Age, List<UserRecord> Friends );
