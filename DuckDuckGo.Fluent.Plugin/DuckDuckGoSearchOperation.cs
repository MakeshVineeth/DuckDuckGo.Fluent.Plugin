using Blast.Core.Results;

namespace DuckDuckGo.Fluent.Plugin;

public enum ActionType
{
    OpenDuckDuckGo,
    SaveImage
}

public sealed class DuckDuckGoSearchOperation : SearchOperationBase
{
    private DuckDuckGoSearchOperation(ActionType actionType, string actionName, string actionDescription,
        string icon)
    {
        ActionType = actionType;
        OperationName = actionName;
        Description = actionDescription;
        IconGlyph = icon;
    }

    public ActionType ActionType { get; }

    public static DuckDuckGoSearchOperation OpenDuck { get; } =
        new(ActionType.OpenDuckDuckGo, "Open URL",
            "Opens the URL. If URL is not available, Opens in DuckDuckGo.", "\uE8A7");

    public static DuckDuckGoSearchOperation SaveImageOperation { get; } =
        new(ActionType.SaveImage, "Save Image", "Stores the Image using Save File Dialog.",
            "\uE74E");
}