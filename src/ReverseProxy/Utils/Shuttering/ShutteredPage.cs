namespace Defra.PackagingWasteProxy.ReverseProxy.Utils.Shuttering;

internal sealed record ShutteredPage(string RouteId, string MatchPath, ReadOnlyMemory<byte> Content);
