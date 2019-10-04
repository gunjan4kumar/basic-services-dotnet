﻿using JohnsonControls.Metasys.BasicServices.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JohnsonControls.Metasys.BasicServices.Interfaces
{
    public interface IMetasysClient
    {
        (string Token, DateTime ExpirationDate) TryLogin(string username, string password, bool refresh = true);
        Task<(string Token, DateTime ExpirationDate)> TryLoginAsync(string username, string password, bool refresh = true);
        (string Token, DateTime ExpirationDate) Refresh();
        Task<Guid> GetObjectIdentifierAsync(string itemReference);
        Task<Variant> ReadPropertyAsync(Guid id, string attributeName);       
        Task<IEnumerable<(Guid Id, IEnumerable<Variant> Variants)>> ReadPropertyMultipleAsync(IEnumerable<Guid> ids,
              IEnumerable<string> attributeNames);
        Task<(string Token, DateTime ExpirationDate)> RefreshAsync();
        (string Token, DateTime ExpirationDate) GetAccessToken();
        Guid GetObjectIdentifier(string itemReference);
        Variant ReadProperty(Guid id, string attributeName);
        IEnumerable<(Guid Id, IEnumerable<Variant> Variants)> ReadPropertyMultiple(IEnumerable<Guid> ids,
            IEnumerable<string> attributeNames);       
    }
}
