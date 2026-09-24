#if UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WorkOutTheme;

/// <summary>Explicit layouts for the existing game screens; preserves their controls and callbacks.</summary>
public static class WorkOutScreenStyle
{
    public static void Session(GameObject canvas, TMP_Text timer)
    {
        var desktop = canvas.GetComponent<WorkOutDesktop>() ?? canvas.AddComponent<WorkOutDesktop>();
        desktop.Initialize(timer);
        Place(canvas.transform.Find("SlotRoot"), 0.24f, 0.05f, 0.98f, 0.83f);
    }

    public static void Score(GameObject canvas, TMP_Text label)
    {
        if (canvas == null || label == null) return;
        ScaleCanvas(canvas);
        canvas.GetComponent<Canvas>().sortingOrder = 120;
        Place(label.transform, 0.4f, 0.006f, 0.6f, 0.038f);
        Text(label, 17, Muted, TextAlignmentOptions.Center);
    }

    public static void Ability(GameObject canvas, Slider bar, Button button)
    {
        if (canvas == null || bar == null || button == null) return;
        ScaleCanvas(canvas);
        var panel = bar.transform.parent;
        Place(panel, 0.03f, 0.225f, 0.207f, 0.335f);
        Skin(panel, Surface.Raised, true).raycastTarget = false;
        Place(bar.transform, 0, 0.56f, 1, 0.88f, 16, 0, 16, 0);
        Slider(bar);
        if (bar.handleRect != null) bar.handleRect.gameObject.SetActive(false);
        Place(button.transform, 0, 0, 1, 0.51f, 12, 10, 12, 0);
        Button(button, Surface.Blue, "Ability");
        foreach (string name in new[] { "SlowIcon", "BlurPanel" })
        {
            var effect = canvas.transform.Find(name);
            Place(effect, 0.24f, 0.05f, 0.98f, 0.83f);
        }
    }

    public static void Puzzle(GameObject root, MonoBehaviour behaviour)
    {
        if (root.transform.Find("DesktopWindow") != null) return;
        // A reparented puzzle uses the session Canvas, including its scaling and sorting.
        var nestedCanvas = root.GetComponent<Canvas>();
        if (nestedCanvas != null) nestedCanvas.overrideSorting = false;
        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;
        if (behaviour is BudgetMiniGame budget) Budget(root.transform, budget);
        else if (behaviour is PaintDrawer paint) Drawing(root.transform, paint);
        else if (behaviour is DocumentApprovalGame) Document(root.transform);
    }

    public static void Budget(Transform root, BudgetMiniGame game)
    {
        var image = root.GetComponent<Image>();
        if (image != null) image.enabled = false;
        Window(root, "Stonks  /  Budget allocation", WorkOutIcon.Symbol.Bars);
        var main = Panel("MainBudgetCard", root, Surface.Raised);
        Place(main, 0, 0.63f, 1, 1, 24, 0, 24, 85);
        Fixed(Icon(main, WorkOutIcon.Symbol.Bars, Green).transform, 25, -20, 30, 30);
        game.totalText = Label("BudgetTotal", main, "BUDGET: 0", 25);
        Place(game.totalText.transform, 0, 0.52f, 1, 1, 72, 0, 25, 7);
        if (game.budgetBar != null)
        {
            game.budgetBar.transform.SetParent(main, false);
            Place(game.budgetBar.transform, 0, 0, 1, 0.5f, 30, 22, 30, 3);
            Slider(game.budgetBar, true);
            game.barColor = Color.white;
            if (game.budgetFill != null) game.budgetFill.color = Color.white;
            if (game.targetZone != null) game.targetZone.SetAsLastSibling();
        }

        Transform oldContainer = root.Find("SlidersContainer");
        if (oldContainer != null)
            foreach (var layout in oldContainer.GetComponents<LayoutGroup>()) layout.enabled = false;
        var channels = Panel("BudgetChannels", root, Surface.Raised);
        Place(channels, 0, 0.12f, 1, 0.6f, 24, 0, 24, 0);
        Place(Label("ScaleLow", channels, "less", 16, Muted).transform, 0.34f, 0.89f, 0.6f, 1, 0, 0, 0, 5);
        Place(Label("ScaleHigh", channels, "more", 16, Muted, TextAlignmentOptions.MidlineRight).transform, 0.73f, 0.89f, 0.96f, 1, 0, 0, 0, 5);
        var symbols = new[] { WorkOutIcon.Symbol.Coffee, WorkOutIcon.Symbol.People, WorkOutIcon.Symbol.Bars, WorkOutIcon.Symbol.Person };
        for (int i = 0; i < game.sliders.Length; i++)
        {
            var entry = game.sliders[i];
            if (entry.slider == null) continue;
            float y = 0.7f - i * 0.205f;
            var row = Rect("BudgetRow" + i, channels);
            Place(row, 0, y, 1, y + 0.18f, 25, 0, 28, 0);
            Place(Icon(row, symbols[i % symbols.Length], PlayerColors[i % 4]).transform, 0, 0.13f, 0.045f, 0.87f);
            entry.valueText = Label("ChannelValue", row, entry.label + ": 0", 22);
            Place(entry.valueText.transform, 0.065f, 0, 0.32f, 1);
            entry.slider.transform.SetParent(row, false);
            Place(entry.slider.transform, 0.34f, 0, 1, 1, 0, 8, 0, 8);
            Slider(entry.slider);
        }
        game.statusText = Label("BudgetHint", root, "Adjust spending to reach the target. Ask your manager for guidance.", 19, Muted);
        Place(game.statusText.transform, 0, 0, 1, 0.11f, 32, 16, 32, 0);
    }

    public static void Drawing(Transform root, PaintDrawer paint)
    {
        var rootText = root.GetComponent<TMP_Text>();
        if (rootText != null) rootText.enabled = false;
        var rootImage = root.GetComponent<Image>();
        if (rootImage != null) rootImage.enabled = false;
        Window(root, "Figma  /  Design studio", WorkOutIcon.Symbol.Palette);
        var left = root.Find("LeftPanel");
        var right = root.Find("RightPanel");
        if (left != null)
        {
            DisableLayouts(left);
            Skin(left, Surface.Inset).raycastTarget = false;
            Place(left, 0, 0, 0.155f, 1, 20, 25, 0, 84);
            var buttons = left.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                bool eraser = button.name.Contains("Eraser");
                Button(button, eraser ? Surface.Raised : Surface.Blue, eraser ? "Eraser" : "Brush");
                Place(button.transform, 0, eraser ? 0.57f : 0.78f, 1, eraser ? 0.74f : 0.95f, 13, 0, 13, 0);
                var icon = Icon(button.transform, eraser ? WorkOutIcon.Symbol.Eraser : WorkOutIcon.Symbol.Pen);
                Place(icon.transform, 0.3f, 0.41f, 0.7f, 0.9f);
                var label = button.GetComponentInChildren<TMP_Text>();
                Place(label.transform, 0, 0, 1, 0.4f, 5, 4, 5, 0);

            }
            Place(Label("ToolsHint", left, "TOOLS", 13, Muted, TextAlignmentOptions.Center).transform, 0, 0.08f, 1, 0.16f, 5, 0, 5, 0);
        }
        if (right != null)
        {
            DisableLayouts(right);
            Skin(right, Surface.Inset).raycastTarget = false;
            Place(right, 0.81f, 0, 1, 1, 0, 25, 20, 84);
            Place(Label("PaletteTitle", right, "Palette", 23).transform, 0, 0.87f, 1, 1, 18, 0, 12, 0);
            var buttons = right.GetComponentsInChildren<Button>(true);
            int index = 0;
            foreach (var button in buttons)
            {
                Button(button, Surface.Tint, "");
                Place(button.transform, 0, 0.72f - index * 0.125f, 1, 0.82f - index * 0.125f, 18, 0, 18, 0);
                Color swatch = button.name.Contains("Red") ? Red : button.name.Contains("Blue") ? Blue : button.name.Contains("Green") ? Green : button.name.Contains("Yellow") ? Hex(0xffd147) : Hex(0x121929);
                button.targetGraphic.color = swatch;
                var colors = button.colors;
                colors.normalColor = swatch;
                colors.highlightedColor = Color.Lerp(swatch, Color.white, 0.25f);
                colors.selectedColor = Color.Lerp(swatch, Color.white, 0.15f);
                colors.pressedColor = Color.Lerp(swatch, Color.black, 0.25f);
                button.colors = colors;

                index++;
            }
            var send = NewButton("SubmitDrawing", right, "Submit", Surface.Green);
            Place(send.transform, 0, 0, 1, 0.15f, 14, 16, 14, 0);
            paint.SetSubmitButton(send);
        }
        var paper = Panel("DrawingPaper", root, Surface.Paper);
        Place(paper, 0.175f, 0, 0.79f, 1, 0, 25, 0, 84);
        paint.transform.SetParent(paper, false);
        Place(paint.transform, 0, 0, 1, 1, 12, 12, 12, 12);
        // Canvas geometry must settle before PaintDrawer allocates its paint texture in Start.
        Canvas.ForceUpdateCanvases();
    }

    public static void Document(Transform root)
    {
        Window(root, "Worb  /  Document approval", WorkOutIcon.Symbol.Document);
        var bg = root.Find("BG");
        if (bg == null) return;
        Skin(bg, Surface.Inset).raycastTarget = false;
        Place(bg, 0, 0, 1, 1, 20, 20, 20, 83);
        foreach (string name in new[] { "Border", "Line", "LineBottom" })
        { var decoration = bg.Find(name); if (decoration != null) decoration.gameObject.SetActive(false); }
        var paperBack = Panel("PaperStackBack", bg, Surface.Paper);
        Place(paperBack, 0.19f, 0.19f, 0.83f, 0.94f);
        paperBack.SetAsFirstSibling();
        var paper = Panel("DocumentPaper", bg, Surface.Paper);
        Place(paper, 0.175f, 0.21f, 0.815f, 0.96f);
        paper.SetSiblingIndex(1);
        var requestNumber = bg.Find("RequestTitle/RequestNumber");
        if (requestNumber != null) requestNumber.SetParent(bg, false);
        DocumentText(bg, "CompanyTitle", 0.06f, 0.82f, 0.94f, 0.94f, 22, true, paper);
        DocumentText(bg, "RequestTitle", 0.06f, 0.67f, 0.94f, 0.8f, 28, true, paper);
        DocumentText(bg, "RequestNumber", 0.06f, 0.58f, 0.94f, 0.68f, 16, false, paper);
        DocumentText(bg, "RequestBody", 0.06f, 0.33f, 0.94f, 0.56f, 24, false, paper);
        DocumentText(bg, "ReasonText", 0.06f, 0.17f, 0.94f, 0.32f, 18, false, paper);
        DocumentText(bg, "AmountText", 0.06f, 0.035f, 0.71f, 0.16f, 22, true, paper);
        var logo = paper.Find("CompanyTitle/Logo");
        if (logo != null) logo.gameObject.SetActive(false);
        foreach (string name in new[] { "SignatureImage", "StampImage", "PhotoImage" })
        {
            var graphic = bg.Find(name);
            if (graphic == null) continue;
            graphic.SetParent(paper, false);
            if (name == "StampImage") Place(graphic, 0.43f, 0.13f, 0.94f, 0.57f);
            else if (name == "SignatureImage") Place(graphic, 0.72f, 0.02f, 0.95f, 0.15f);
            else Place(graphic, 0.82f, 0.59f, 0.94f, 0.74f);
            var img = graphic.GetComponent<Image>();
            if (img != null) { img.raycastTarget = false; img.preserveAspect = true; }
        }
        foreach (var button in bg.GetComponentsInChildren<Button>(true))
        {
            bool approve = button.name.Trim().StartsWith("Approve");
            Button(button, approve ? Surface.Green : Surface.Red, approve ? "APPROVE" : "REJECT");
            Place(button.transform, approve ? 0.06f : 0.55f, 0.035f, approve ? 0.45f : 0.94f, 0.145f);
        }
        var feedback = root.Find("FeedbackPanel");
        if (feedback != null)
        {
            Place(feedback, 0.2f, 0.38f, 0.8f, 0.63f);
            Skin(feedback, Surface.Header, true);
            foreach (var label in feedback.GetComponentsInChildren<TMP_Text>(true))
            { Text(label, 28, Ink, TextAlignmentOptions.Center); Place(label.transform, 0, 0, 1, 1, 22, 20, 22, 20); }
        }
    }

    private static void DocumentText(Transform bg, string name, float x0, float y0, float x1, float y1, float size, bool bold, Transform paper)
    {
        var t = bg.Find(name);
        if (t == null) return;
        t.SetParent(paper, false);
        Place(t, x0, y0, x1, y1);
        var text = t.GetComponent<TMP_Text>();
        Text(text, size, PaperInk);
        if (text != null) text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
    }

    public static void Boss(Transform root)
    {
        Session(root.gameObject, null);
        var oldCameras = root.Find("LeftCameras");
        if (oldCameras != null) oldCameras.gameObject.SetActive(false);
        var panels = root.Find("RightPanels");
        if (panels == null || panels.Find("DesktopWindow") != null) return;
        Place(panels, 0.24f, 0.05f, 0.98f, 0.83f);
        Window(panels, "Work Out Inc.  /  Team desktops", WorkOutIcon.Symbol.Monitor);
        var budget = panels.Find("BudgetPanel");
        var artist = panels.Find("ArtistPlaceholderPanel");
        var documents = panels.Find("CopywriterPanel");
        var reserve = Rect("ReservePanel", panels);
        Transform[] cells = { budget, artist, documents, reserve };
        string[] titles = { "Stonks · Budget", "Figma · Design", "Worb · Reference", "Teamwork" };
        WorkOutIcon.Symbol[] symbols = { WorkOutIcon.Symbol.Bars, WorkOutIcon.Symbol.Palette, WorkOutIcon.Symbol.Document, WorkOutIcon.Symbol.People };
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) continue;
            float x = (i % 2) * 0.5f, y = i < 2 ? 0.455f : 0;
            Place(cells[i], x, y, x + 0.5f, y + 0.455f, i % 2 == 0 ? 18 : 8, 18, i % 2 == 0 ? 8 : 18, 0);
            var text = cells[i].GetComponent<TMP_Text>();
            if (text != null) text.enabled = false;
            Window(cells[i], titles[i], symbols[i]);
            var header = cells[i].Find("DesktopWindow/WindowHeader");
            Place(header, 0, 1, 1, 1, 0, -53, 0, 0);
            Text(header.GetComponentInChildren<TMP_Text>(), 22);
        }
        if (budget != null)
        {
            Place(Label("BudgetHelp", budget, "The green mark is the target.\nGuide your programmer to it.", 20, Muted).transform, 0, 0.46f, 1, 0.78f, 25, 0, 25, 0);
            var bar = budget.Find("BudgetBar");
            if (bar != null) { Place(bar, 0, 0.32f, 1, 0.43f, 30, 0, 30, 0); Skin(bar, Surface.Track); }
            var confirm = budget.Find("ConfirmButton");
            if (confirm != null) { Place(confirm, 0, 0, 1, 0.22f, 25, 18, 25, 0); Button(confirm.GetComponent<Button>()); }
        }
        if (artist != null)
        {
            Place(Icon(artist, WorkOutIcon.Symbol.Pen, Blue).transform, 0.38f, 0.35f, 0.62f, 0.68f);
            Place(Label("ArtistHint", artist, "Discuss the layout with your designer", 20, Muted, TextAlignmentOptions.Center).transform, 0, 0.12f, 1, 0.32f, 20, 0, 20, 0);
        }
        var list = root.Find("DocumentListPanel");
        if (list != null && documents != null) list.SetParent(documents, false);
        if (documents != null)
        {
            list = documents.Find("DocumentListPanel");
            Place(list, 0, 0, 1, 1, 14, 14, 14, 65);
        }
        Place(Icon(reserve, WorkOutIcon.Symbol.People, Green).transform, 0.39f, 0.37f, 0.61f, 0.68f);
        Place(Label("TeamHint", reserve, "Watch the clock.\nHelp each other finish tasks.", 20, Muted, TextAlignmentOptions.Center).transform, 0, 0.08f, 1, 0.33f, 22, 0, 22, 0);
    }

    public static void Lobby(GameObject canvas)
    {
        ScaleCanvas(canvas);
        var join = canvas.transform.Find("CreateJoinPanel");
        if (join != null && join.Find("BrandTitle") == null)
        {
            Skin(join, Surface.Inset).raycastTarget = false;
            Place(Label("BrandTitle", join, "WORK\nOUT INC.", 84).transform, 0.08f, 0.46f, 0.5f, 0.88f);
            Place(Label("BrandSubtitle", join, "One team. Four roles.\nFinish the work before your shift ends.", 26, Muted).transform, 0.085f, 0.28f, 0.49f, 0.46f);
            var card = Panel("ConnectionCard", join);
            Place(card, 0.55f, 0.16f, 0.93f, 0.84f);
            card.SetAsFirstSibling();
            Place(Label("JoinTitle", card, "Start your shift", 38).transform, 0, 0.79f, 1, 0.95f, 35, 0, 30, 0);
            var create = join.Find("CreateButton");
            Place(create, 0.575f, 0.57f, 0.905f, 0.66f);
            if (create != null) Button(create.GetComponent<Button>(), Surface.Blue, "Create a team");
            Place(Label("JoinCodeHint", join, "Already invited? Enter the lobby code", 19, Muted).transform, 0.575f, 0.475f, 0.905f, 0.54f);
            var input = join.Find("CodeInputField");
            if (input != null)
            {
                Place(input, 0.575f, 0.39f, 0.905f, 0.475f);
                Skin(input, Surface.Inset);
                var field = input.GetComponent<TMP_InputField>();
                if (field != null)
                {
                    Text(field.textComponent, 28);
                    Text(field.placeholder as TMP_Text, 23, Muted);
                    if (field.placeholder is TMP_Text placeholder) placeholder.text = "Invite code";
                }
            }
            var connect = join.Find("JoinButton");
            Place(connect, 0.575f, 0.275f, 0.905f, 0.36f);
            if (connect != null) Button(connect.GetComponent<Button>(), Surface.Green, "Join team");
            var status = join.Find("StatusText");
            Place(status, 0.575f, 0.18f, 0.905f, 0.26f);
            if (status != null) { Text(status.GetComponent<TMP_Text>(), 18, Muted); status.GetComponent<TMP_Text>().text = "Invite your team and choose your roles"; }
        }
        var lobby = canvas.transform.Find("LobbyPanel");
        if (lobby == null || lobby.Find("DesktopWindow") != null) return;
        Skin(lobby, Surface.Inset).raycastTarget = false;
        Window(lobby, "Work Out Inc.  /  Team lobby", WorkOutIcon.Symbol.People);
        Place(lobby.Find("PlayerProfiles"), 0.04f, 0.16f, 0.47f, 0.77f);
        ConfigureList(lobby.Find("PlayerProfiles"));
        Place(lobby.Find("LobbyCode"), 0.05f, 0.8f, 0.63f, 0.88f);
        var codeLabel = lobby.Find("LobbyCode")?.GetComponent<TMP_Text>();
        if (codeLabel != null) codeLabel.text = "Invite your team using the lobby code";
        Place(lobby.Find("PlayerRole"), 0.54f, 0.69f, 0.93f, 0.77f);
        string[] roles = { "Role1", "Role2", "Role3" };
        string[] captions = { "Copywriter  /  Documents", "Designer  /  Coloring", "Programmer  /  Budget" };
        for (int i = 0; i < 3; i++)
        {
            var button = lobby.Find(roles[i]);
            if (button == null) continue;
            Place(button, 0.54f, 0.54f - i * 0.13f, 0.93f, 0.65f - i * 0.13f);
            Button(button.GetComponent<Button>(), Surface.Raised, captions[i]);
        }
        var action = lobby.Find("ReadyStartButton");
        Place(action, 0.68f, 0.05f, 0.93f, 0.15f);
        if (action != null) Button(action.GetComponent<Button>(), Surface.Green);
        var settings = lobby.Find("Button");
        Place(settings, 0.76f, 0.8f, 0.93f, 0.87f);
        if (settings != null) Button(settings.GetComponent<Button>());
        foreach (var label in lobby.GetComponentsInChildren<TMP_Text>(true)) Text(label, 25, null, label.alignment);
    }

    public static void Results(GameObject canvas)
    {
        ScaleCanvas(canvas);
        if (canvas.transform.Find("DesktopWindow") != null) return;
        Window(canvas.transform, "Work Out Inc.  /  Shift results", WorkOutIcon.Symbol.Check);
        Place(canvas.transform.Find("Text (TMP)"), 0.06f, 0.76f, 0.94f, 0.87f);
        Place(canvas.transform.Find("ResultsListContainer"), 0.06f, 0.23f, 0.94f, 0.73f);
        ConfigureList(canvas.transform.Find("ResultsListContainer"));
        Place(canvas.transform.Find("TotalCurrencyText"), 0.06f, 0.07f, 0.55f, 0.18f);
        var button = canvas.transform.Find("ReturnToLobbyButton");
        Place(button, 0.64f, 0.07f, 0.94f, 0.17f);
        if (button != null) Button(button.GetComponent<Button>(), Surface.Blue, "Back to lobby");
        foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true)) Text(label, 28, null, label.alignment);
    }

    public static void ListRow(GameObject row)
    {
        Skin(row.transform, Surface.Raised).raycastTarget = false;
        var avatar = row.transform.Find("PlayerImage");
        if (avatar != null)
        {
            Skin(avatar, Surface.Blue).raycastTarget = false;
            Fixed(avatar, 16, -12, 52, 52);
            Place(Icon(avatar, WorkOutIcon.Symbol.Person).transform, 0.15f, 0.1f, 0.85f, 0.9f);
        }
        foreach (var label in row.GetComponentsInChildren<TMP_Text>(true))
        { Text(label, 21); Place(label.transform, 0, 0, 1, 1, avatar != null ? 88 : 22, 7, 22, 7); }
        var element = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
        element.minHeight = 66;
        element.preferredHeight = 80;
    }

    private static void DisableLayouts(Transform target)
    {
        foreach (var layout in target.GetComponents<LayoutGroup>()) layout.enabled = false;
        foreach (var fitter in target.GetComponents<ContentSizeFitter>()) fitter.enabled = false;
    }

    private static void SelectDrawingTool(Transform root, bool eraser)
    {
        var tools = root.Find("LeftPanel");
        if (tools == null) return;
        foreach (var button in tools.GetComponentsInChildren<Button>())
            Skin(button.transform, button.name.Contains("Eraser") == eraser ? Surface.Blue : Surface.Raised, true);
    }

    private static void ConfigureList(Transform target)
    {
        if (target == null) return;
        var background = target.GetComponent<Image>();
        if (background != null) { Skin(target, Surface.Inset); background.raycastTarget = false; }
        var layout = target.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }
}

#endif
