using System.Collections.Generic;

namespace CfSampleAppDotNetCore.Models
{
    public interface IKittenRepository
    {
        Kitten Create(Kitten kitten);
        List<string> Find();
    }
}
