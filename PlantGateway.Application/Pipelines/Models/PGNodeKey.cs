using System;
using System.Collections.Generic;
using System.Text;

namespace PlantGateway.Application.Pipelines.Models
{
    /// <summary>
    /// Known Plant Gateway node types appearing in import/export documents.
    /// Used as keys to avoid string typos and improve consistency.
    /// </summary>
    public enum PGNodeKey
    {
        Unset,
        Root,
        Header,
        Info,
        Export,
        Units,
        Assembly,
        Part,
        Matrix,
        GlobalMatrix
    }
}
