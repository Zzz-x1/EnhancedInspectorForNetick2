using UnityEditor;
using UnityEngine;

namespace Netick.Unity.Editor.Addon
{

    internal static class Icons
    {
        public static GUIContent GetContent(string iconName, string tooltip = null)
        {
            return EditorGUIUtility.TrIconContent(iconName, tooltip);
        }

        public static GUIContent GetContentWithText(string iconName, string text, string tooltip = null)
        {
            return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, iconName);
        }

        public static string GetPath(string name)
        {
            return $"Packages/com.karrar.netick/Netick/Editor/Resources/{name}.png";
        }

        public static Texture2D GetIconTexture(string iconName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GetPath(iconName));
        }
    }

    internal static class GUIContents
    {
        public static readonly string logoPath = AssetDatabase.GUIDToAssetPath("0170f1b3d9b4ffe4fbd79ae4c7533f99"); // path to logo texture
        public static readonly GUIContent syncWithOn = Icons.GetContentWithText(logoPath, "<color=#29abe2>Sync with </color>"); // text color matches Logo.Icon color
        public static readonly GUIContent syncWithOff = Icons.GetContentWithText(logoPath, "<color=#868686>Sync with </color>"); // text color matches Logo.Icon.Disabled color
        public static readonly GUIContent netickBanner = Icons.GetContent(logoPath);
        public static readonly GUIContent openInWindow = EditorGUIUtility.TrIconContent("ToolSettings", "Open Networked Prefabs");
        public static readonly GUIContent selectRoots = EditorGUIUtility.TrIconContent("Update-Available", "Select the Root(s)");
    }

    internal class GUIStyles
    {
        public static readonly GUIStyle wrappedLabel = new(EditorStyles.label)
        {
            wordWrap = true,
        };

        public static readonly GUIStyle richLabel = new(wrappedLabel)
        {
            richText = true,
        };

        /// <summary>
        /// Like <see cref="richLabel"/>, but text color is pure white (on both Dark and Light themes).
        /// </summary>
        public static readonly GUIStyle richWhiteLabel = new(richLabel);

        public static readonly GUIStyle richLabelNoWrap = new(richLabel)
        {
            wordWrap = false,
        };

        public static readonly GUIStyle miniLabel = new(EditorStyles.miniLabel)
        {
            wordWrap = true,
        };

        public static readonly GUIStyle labelNoExpand = new(EditorStyles.label)
        {
            wordWrap = true,
            stretchWidth = false,
        };

        public static readonly GUIStyle richMiniLabel = new(miniLabel)
        {
            richText = true,
            wordWrap = true,
        };

        public static readonly GUIStyle richMiniLabelNoWrap = new(miniLabel)
        {
            richText = true,
            wordWrap = false,
        };

        public static readonly GUIStyle richToggle = new(EditorStyles.toggle)
        {
            richText = true,
        };

        public static readonly GUIStyle centeredGreyMiniLabelWrap = new(EditorStyles.centeredGreyMiniLabel)
        {
            wordWrap = true,
        };

        public static readonly GUIStyle centeredLabelWrap = new(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };

        public static readonly GUIStyle verticallyCenteredRowLabel = new(EditorStyles.label)
        {
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(),
        };

        public static readonly GUIStyle centeredStretchedLabel = new(centeredGreyMiniLabelWrap)
        {
            stretchWidth = true,
            stretchHeight = true,
        };

        public static readonly GUIStyle centeredStretchedTinyLabel = new(centeredStretchedLabel)
        {
            fontSize = 6,
            fontStyle = FontStyle.Bold,
        };

        public static readonly GUIStyle miniLabelRight = new(richMiniLabel)
        {
            alignment = TextAnchor.MiddleRight,
        };

        public static readonly GUIStyle greyMiniLabelRight = new(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleRight,
        };

        public static readonly GUIStyle centeredLabelTopImage = new(centeredStretchedLabel)
        {
            imagePosition = ImagePosition.ImageAbove,
        };

        public static readonly GUIStyle centeredMiniLabel = new(richMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
        };

        public static readonly GUIStyle statusBar = new("ProjectBrowserBottomBarBg")
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(4, 4, 0, 0),
        };

        public static readonly GUIStyle miniLabelGrey = new(EditorStyles.miniLabel)
        {
            normal = new GUIStyleState
            {
                textColor = Color.grey,
            },
            hover = new GUIStyleState
            {
                textColor = Color.grey,
            },
        };

        public static readonly GUIStyle miniLabelGreyWrap = new(miniLabelGrey)
        {
            wordWrap = true,
        };

        public static readonly GUIStyle wordWrappedMiniLabelHighlight = new(EditorStyles.wordWrappedMiniLabel)
        {
            normal = new GUIStyleState
            {
                textColor = Color.cyan,
            },
        };

        public static readonly GUIStyle miniButtonLeftPressed = new(EditorStyles.miniButtonLeft)
        {
            normal = EditorStyles.miniButtonLeft.hover,
        };

        public static readonly GUIStyle statusBarButton = new(EditorStyles.toolbarButton)
        {
            margin = new RectOffset(0, 0, 1, 0),
            padding = new RectOffset(
                EditorStyles.toolbarButton.padding.left,
                EditorStyles.toolbarButton.padding.right,
                EditorStyles.toolbarButton.padding.top - 1,
                EditorStyles.toolbarButton.padding.bottom),
        };

        public static readonly GUIStyle toolbarDropDownToggle = new("toolbarDropDownToggle");
        public static readonly GUIStyle toolbarDropDownToggleRight = new("toolbarDropDownToggleRight");
        public static readonly GUIStyle toolbarDropDownToggleButton = new("toolbarDropDownToggleButton");
        public static readonly GUIStyle toolbarDropDown = new("toolbarDropDown");
        public static readonly GUIStyle toolbarDropDownLeft = new("toolbarDropDownLeft");
        public static readonly GUIStyle toolbarDropDownRight = new("toolbarDropDownRight");

        public static readonly GUIStyle iconButton = new("IconButton")
        {
            stretchWidth = false,
        };

        public static readonly GUIStyle copyToClipboardButton = new(EditorStyles.label)
        {
            stretchWidth = false
        };

        public static readonly GUIStyle iconButtonPadded = new(iconButton)
        {
            margin = new RectOffset(0, 0, 2, 0),
        };

        public static readonly GUIStyle menuItem = new("MenuItem")
        {
            imagePosition = ImagePosition.ImageLeft,
            fixedHeight = 19,
        };

        public static readonly GUIStyle miniMenuItem = new("MenuItem")
        {
            padding = new RectOffset(menuItem.padding.left, menuItem.padding.right, 0, 0),
            fixedHeight = 14,
            fontSize = EditorStyles.miniLabel.fontSize,
        };

        public static readonly GUIStyle separator = new("sv_iconselector_sep");

        public static readonly GUIStyle frameBox = new("FrameBox");

        public static readonly GUIStyle tabOnlyOne = new("Tab onlyOne");
        public static readonly GUIStyle tabFirst = new("Tab first");
        public static readonly GUIStyle tabMiddle = new("Tab middle");
        public static readonly GUIStyle tabLast = new("Tab last");

        public static readonly GUIStyle boldButton = new(EditorStyles.miniButton)
        {
            fontStyle = FontStyle.Bold,
        };

        public static readonly GUIStyle header = new(EditorStyles.helpBox)
        {
            margin = new RectOffset(2, 2, 12, 2),
        };

        public static readonly GUIStyle toolbarButton = new(EditorStyles.toolbarButton)
        {
            stretchWidth = false,
            stretchHeight = false,
        };

        public static readonly GUIStyle bigButton = new(EditorStyles.miniButton)
        {
            fixedHeight = 32,
        };

        public static readonly GUIStyle bigBoldButton = new(bigButton)
        {
            fontStyle = FontStyle.Bold,
        };

        public static readonly GUIStyle fitButton = new(EditorStyles.miniButton)
        {
            stretchWidth = false,
        };

        /// <summary>
        /// GUIStyle for a mini button just wide enough to contain the text "Save".
        /// </summary>
        public static readonly GUIStyle saveButton = new(EditorStyles.miniButton)
        {
            fixedWidth = 42,
            stretchWidth = false
        };

        public static readonly GUIStyle toolbarSearchCancel;
        public static readonly GUIStyle toolbarSearchCancelEmpty;

        public static readonly GUIStyle sineScrollerLabel = new()
        {
            fontStyle = FontStyle.Bold,
            normal =
                {
                    textColor = Color.white,
                }
        };

        static GUIStyles()
        {
            // At some point during 2021.3 LTS Unity renamed the internal style ToolbarSeachCancelButton to
            // ToolbarSearchCancelButton. For compatibility purposes, we default to the new name, but fallback to
            // the old one.
            toolbarSearchCancel = GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? GUI.skin.FindStyle("ToolbarSeachCancelButton");
            toolbarSearchCancelEmpty = GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty") ?? GUI.skin.FindStyle("ToolbarSeachCancelButtonEmpty");

            richWhiteLabel.normal.textColor = Color.white;
            richWhiteLabel.hover.textColor = Color.white;
            richWhiteLabel.active.textColor = Color.white;
            richWhiteLabel.focused.textColor = Color.white;
        }
    }
}