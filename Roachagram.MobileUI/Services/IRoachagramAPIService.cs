using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roachagram.MobileUI.Services
{
    public interface IRoachagramAPIService
    {
        Task<string> GetAnagramsAsync(string input);
    }
}
