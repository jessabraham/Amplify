using System;
using System.Collections.Generic;
using System.Text;

namespace Amplify.Domain.Interfaces.Common
{
    public interface IEntity
    {
        Guid Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }

}
