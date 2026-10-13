using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 自动构建鬼屋主菜单场景（Tools 菜单 -> 构建鬼屋主菜单）。
/// 生成暗角 / 光点贴图，搭建 Canvas、标题、按钮、操作说明面板、环境音，
/// 保存为 Assets/Scenes/MainMenu.unity 并加入 Build Settings 首位。
/// </summary>
public static class MenuBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string BgPath = "Assets/Textures/UI/MainMenu_BG.png";
    const string ButtonPath = "Assets/Textures/UI/MenuButton.png";
    const string VignettePath = "Assets/Textures/UI/MenuVignette.png";
    const string GlowPath = "Assets/Textures/UI/MenuGlowDot.png";
    const string TitleFontPath = "Assets/Fonts/Lishu.ttf";
    const string BodyFontPath = "Assets/Fonts/Hei.ttf";
    const string AmbientPath = "Assets/Audio/SFXHouseAmbience.wav";

    static readonly Color GoldColor = new Color(0.98f, 0.82f, 0.5f, 1f);
    static readonly Color TextColor = new Color(0.96f, 0.9f, 0.78f, 1f);

    [MenuItem("Tools/构建鬼屋主菜单 %#m")]
    public static void BuildMenu()
    {
        try
        {
            EnsureSprite(BgPath, 0);
            EnsureSprite(ButtonPath, 90);
            GenerateVignette();
            GenerateGlowDot();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var fontTitle = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            var fontBody = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            var vigSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
            var ambientClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientPath);

            if (fontTitle == null || fontBody == null || bgSprite == null || btnSprite == null)
                throw new Exception("关键资源加载失败，请检查字体与图片是否已导入。");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();

            // ---------- Canvas ----------
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var rootGroup = canvasGo.AddComponent<CanvasGroup>();

            // ---------- 背景 ----------
            var bg = CreateImage("Background", bgSprite, canvasGo.transform);
            Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;

            var vig = CreateImage("Vignette", vigSprite, canvasGo.transform);
            Stretch(vig.rectTransform);
            vig.raycastTarget = false;

            BuildTitle(canvasGo.transform, fontTitle, fontBody);
            var buttons = BuildButtons(canvasGo.transform, fontBody, btnSprite, glowSprite, out Button startButton);
            var helpPanel = BuildHelpPanel(canvasGo.transform, fontTitle, fontBody, btnSprite, glowSprite);
            BuildBottomHint(canvasGo.transform, fontBody);

            // ---------- 环境音 ----------
            var audioGo = new GameObject("MenuAudio");
            var ambientSrc = audioGo.AddComponent<AudioSource>();
            ambientSrc.clip = ambientClip;
            ambientSrc.loop = true;
            ambientSrc.playOnAwake = false;
            ambientSrc.volume = 0f;
            var sfxSrc = audioGo.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false;
            sfxSrc.volume = 0.9f;

            // ---------- 主菜单控制器 ----------
            var menu = canvasGo.AddComponent<MainMenu>();
            menu.rootGroup = rootGroup;
            menu.helpPanel = helpPanel;
            menu.ambientSource = ambientSrc;
            menu.sfxSource = sfxSrc;

            // 按钮事件
            UnityEventTools.AddPersistentListener(startButton.onClick, menu.StartGame);
            UnityEventTools.AddPersistentListener(buttons[1].onClick, menu.ToggleHelp);
            UnityEventTools.AddPersistentListener(buttons[2].onClick, menu.QuitGame);

            // EventSystem 默认选中开始按钮，支持键盘 / 手柄
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null) eventSystem.firstSelectedGameObject = startButton.gameObject;

            // ---------- 保存场景 ----------
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // ---------- Build Settings：主菜单在 0，游戏场景在 1 ----------
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[MenuBuilder] 鬼屋主菜单构建完成：" + ScenePath);
        }
        catch (Exception e)
        {
            Debug.LogError("[MenuBuilder] 构建失败：" + e);
            EditorApplication.Exit(1);
        }
    }

    // ============================== 场景物体 ==============================

    static void BuildCamera()
    {
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        // URP 附加相机数据（反射，避免硬依赖）
        var urpCamType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData"))
            .FirstOrDefault(t => t != null);
        if (urpCamType != null && camGo.GetComponent(urpCamType) == null)
            camGo.AddComponent(urpCamType);
    }

    static void BuildEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        // 项目 Active Input Handling = Both，游戏移动代码用旧输入系统；
        // StandaloneInputModule 即插即用，避免 InputSystemUIInputModule 未绑定 actions 导致按钮无响应。
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static void BuildTitle(Transform parent, Font fontTitle, Font fontBody)
    {
        var group = NewRect("TitleGroup", parent);
        SetAnchors(group, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        group.anchoredPosition = new Vector2(0f, -58f);
        group.sizeDelta = new Vector2(1500f, 300f);

        // 主标题
        var title = CreateText("Title", group.transform, "鬼屋潜行", fontTitle, 132, GoldColor, TextAnchor.MiddleCenter);
        SetAnchors(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        title.rectTransform.sizeDelta = new Vector2(1300f, 190f);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        AddTextShadow(title, new Color(0f, 0f, 0f, 0.9f), new Vector2(4f, -4f));
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.05f, 0f, 0.9f);
        outline.effectDistance = new Vector2(-2f, 2f);
        var floatFx = title.gameObject.AddComponent<TitleFloat>();
        floatFx.glowText = title;

        // 副标题（英文原名）
        var sub = CreateText("Subtitle", group.transform, "J O H N   L E M O N ' S   ·   H A U N T E D   J A U N T",
            fontBody, 30, new Color(0.82f, 0.7f, 0.5f, 1f), TextAnchor.MiddleCenter);
        SetAnchors(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        sub.rectTransform.anchoredPosition = new Vector2(0f, -205f);
        sub.rectTransform.sizeDelta = new Vector2(1500f, 46f);
        sub.horizontalOverflow = HorizontalWrapMode.Overflow;
        AddTextShadow(sub, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));
    }

    static List<Button> BuildButtons(Transform parent, Font font, Sprite btnSprite, Sprite glowSprite, out Button startButton)
    {
        var group = NewRect("ButtonGroup", parent);
        SetAnchors(group, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        group.anchoredPosition = new Vector2(0f, -110f);
        group.sizeDelta = new Vector2(560f, 420f);
        var layout = group.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var result = new List<Button>
        {
            CreateMenuButton("开始游戏", font, btnSprite, glowSprite, group.transform, new Vector2(460f, 96f), 42),
            CreateMenuButton("操作说明", font, btnSprite, glowSprite, group.transform, new Vector2(460f, 96f), 42),
            CreateMenuButton("退出游戏", font, btnSprite, glowSprite, group.transform, new Vector2(460f, 96f), 42)
        };
        startButton = result[0];
        return result;
    }

    static CanvasGroup BuildHelpPanel(Transform parent, Font fontTitle, Font fontBody, Sprite btnSprite, Sprite glowSprite)
    {
        var panelRoot = NewRect("HelpPanel", parent);
        Stretch(panelRoot);
        var cg = panelRoot.gameObject.AddComponent<CanvasGroup>();
        // 初始隐藏且不拦截射线，防止挡住下方按钮（运行时 MainMenu 也会再设置一次）
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 全屏压暗
        var dim = CreateImage("Dim", null, panelRoot.transform);
        Stretch(dim.rectTransform);
        dim.color = new Color(0f, 0f, 0f, 0.72f);

        // 木牌面板
        var panel = CreateImage("Panel", btnSprite, panelRoot.transform);
        panel.type = Image.Type.Sliced;
        SetAnchors(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.rectTransform.sizeDelta = new Vector2(1020f, 700f);

        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(80, 80, 60, 60);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var helpTitle = CreateText("HelpTitle", panel.transform, "操 作 说 明", fontTitle, 56, GoldColor, TextAnchor.MiddleCenter);
        helpTitle.rectTransform.sizeDelta = new Vector2(0f, 80f);
        AddTextShadow(helpTitle, new Color(0f, 0f, 0f, 0.85f), new Vector2(3f, -3f));
        AddLayoutHeight(helpTitle, 80f);

        string[] lines =
        {
            "·  使用 W A S D 或方向键控制约翰在鬼屋中移动",
            "·  白色幽灵会沿着走廊来回巡逻，请躲开它们的路线",
            "·  石像鬼会在原地转动视线扫视，别走进它的视野",
            "·  被发现就会被抓住，需要重新开始本轮挑战",
            "·  抵达走廊尽头透出烛光的出口，即可成功逃出",
            "·  按 Esc 可关闭本面板（游戏中按 Esc 将退出游戏）"
        };

        foreach (var line in lines)
        {
            var t = CreateText("Line", panel.transform, line, fontBody, 28, TextColor, TextAnchor.MiddleLeft);
            t.rectTransform.sizeDelta = new Vector2(0f, 46f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            AddTextShadow(t, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));
            AddLayoutHeight(t, 46f);
        }

        var spacer = NewRect("Spacer", panel.transform);
        spacer.sizeDelta = new Vector2(0f, 10f);
        AddLayoutHeight(spacer, 10f);

        var back = CreateMenuButton("返　回", fontBody, btnSprite, glowSprite, panel.transform, new Vector2(300f, 84f), 36);
        var mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenu>();
        if (mainMenu != null) UnityEventTools.AddPersistentListener(back.onClick, mainMenu.HideHelp);

        return cg;
    }

    static void BuildBottomHint(Transform parent, Font font)
    {
        var hint = CreateText("BottomHint", parent,
            "烛光是你唯一的方向，屏住呼吸穿过鬼屋　——　建议佩戴耳机游玩",
            font, 22, new Color(1f, 1f, 1f, 0.62f), TextAnchor.MiddleCenter);
        SetAnchors(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        hint.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        hint.rectTransform.sizeDelta = new Vector2(1400f, 34f);
        hint.horizontalOverflow = HorizontalWrapMode.Overflow;

        var version = CreateText("Version", parent, "v1.0", font, 18, new Color(1f, 1f, 1f, 0.4f), TextAnchor.LowerRight);
        SetAnchors(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        version.rectTransform.anchoredPosition = new Vector2(-28f, 20f);
        version.rectTransform.sizeDelta = new Vector2(160f, 30f);
    }

    // ============================== UI 工具方法 ==============================

    static Button CreateMenuButton(string label, Font font, Sprite btnSprite, Sprite glowSprite,
        Transform parent, Vector2 size, int fontSize)
    {
        var go = NewRect("Btn_" + label, parent);
        go.sizeDelta = size;

        var img = go.gameObject.AddComponent<Image>();
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = go.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.12f, 0.98f, 1f);
        colors.pressedColor = new Color(0.78f, 0.68f, 0.52f, 1f);
        colors.selectedColor = new Color(1.1f, 1.05f, 0.92f, 1f);
        colors.fadeDuration = 0.12f;
        btn.colors = colors;

        var text = CreateText("Label", go.transform, label, font, fontSize,
            new Color(0.96f, 0.88f, 0.66f, 1f), TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        AddTextShadow(text, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));

        // 两侧光点
        var dots = new List<Image>();
        foreach (var side in new[] { -1f, 1f })
        {
            var dot = CreateImage("GlowDot", glowSprite, go.transform);
            dot.raycastTarget = false;
            dot.rectTransform.sizeDelta = new Vector2(34f, 34f);
            dot.rectTransform.anchoredPosition = new Vector2(side * (size.x * 0.5f + 34f), 0f);
            dot.color = new Color(1f, 0.85f, 0.45f, 0f);
            dots.Add(dot);
        }

        var fx = go.gameObject.AddComponent<MenuButtonFX>();
        fx.glowDots = dots.ToArray();
        return btn;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static Image CreateImage(string name, Sprite sprite, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        if (sprite == null) img.color = Color.white;
        return img;
    }

    static Text CreateText(string name, Transform parent, string content, Font font,
        int fontSize, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void AddTextShadow(Text t, Color color, Vector2 distance)
    {
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    static void AddLayoutHeight(Component c, float height)
    {
        var le = c.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
    }

    // ============================== 贴图生成 ==============================

    static void EnsureSprite(string path, float border)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) throw new FileNotFoundException("找不到贴图：" + path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        if (border > 0f) ti.spriteBorder = new Vector4(border, border, border, border);
        ti.SaveAndReimport();
    }

    /// <summary>径向暗角 + 上下压暗，用于突出菜单中心。</summary>
    static void GenerateVignette()
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        var center = new Vector2(size / 2f, size / 2f);
        const float maxDist = size * 0.72f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float edge = Mathf.Clamp01((d - 0.35f) / 0.65f);
                edge *= edge;

                // 顶部、底部额外压暗
                float vy = Mathf.Abs((y / (float)size) - 0.5f) * 2f; // 0 中 -> 1 边
                float vertical = Mathf.Pow(vy, 2.6f) * 0.55f;

                byte a = (byte)Mathf.Clamp((edge * 0.78f + vertical) * 235f, 0f, 235f);
                pixels[y * size + x] = new Color32(0, 0, 0, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ToAbsolutePath(VignettePath), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(VignettePath, ImportAssetOptions.ForceUpdate);
        EnsureSprite(VignettePath, 0f);
    }

    /// <summary>暖金色径向光点，用作按钮悬停装饰。</summary>
    static void GenerateGlowDot()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        var center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float k = Mathf.Clamp01(1f - d);
                k = k * k;
                byte a = (byte)(k * 255f);
                pixels[y * size + x] = new Color32((byte)255, (byte)226, (byte)150, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ToAbsolutePath(GlowPath), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(GlowPath, ImportAssetOptions.ForceUpdate);
        EnsureSprite(GlowPath, 0f);
    }

    static string ToAbsolutePath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
