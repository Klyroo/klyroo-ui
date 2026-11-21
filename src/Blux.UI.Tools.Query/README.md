# Blux.UI.Tools.Query

The Blazor-free half of [Blux.UI.Tools](https://www.nuget.org/packages/Blux.UI.Tools): the request, filter and result
records that a data list sends to a provider, and the code that applies them to an `IQueryable` or to an in-memory list.
It has no ASP.NET Core and no Blux.UI dependency, so an API server can reference it to answer the same requests the
components make.

This package is a scaffold: the types arrive with the data list.
