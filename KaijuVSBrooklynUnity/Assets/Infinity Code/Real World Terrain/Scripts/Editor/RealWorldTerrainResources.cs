/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEditor;
using UnityEngine;

public static class RealWorldTerrainResources
{
    private static Texture2D _deleteIcon;
    private static Texture2D _helpIcon;
    private static GUIStyle _helpStyle;
    private static Texture2D _openIcon;
    private static Texture2D _saveIcon;

    public static Texture2D deleteIcon
    {
        get
        {
            if (_deleteIcon == null)
            {
                _deleteIcon =
                    (Texture2D)
                        AssetDatabase.LoadAssetAtPath("Assets/Infinity Code/Real World Terrain/Icons/TrashIcon.png",
                            typeof (Texture2D));
            }
            return _deleteIcon;
        }
    }

    public static Texture2D helpIcon
    {
        get
        {
            if (_helpIcon == null)
            {
                _helpIcon =
                    (Texture2D)
                        AssetDatabase.LoadAssetAtPath("Assets/Infinity Code/Real World Terrain/Icons/HelpIcon.png",
                            typeof(Texture2D));
            }
            return _helpIcon;
        }
    }

    public static GUIStyle helpStyle
    {
        get
        {
            if (_helpStyle == null)
            {
                _helpStyle = new GUIStyle();
                _helpStyle.margin = new RectOffset(0, 0, 2, 0);
            }
            return _helpStyle;
        }
    }

    public static Texture2D openIcon
    {
        get
        {
            if (_openIcon == null)
            {
                _openIcon =
                    (Texture2D)
                        AssetDatabase.LoadAssetAtPath("Assets/Infinity Code/Real World Terrain/Icons/OpenIcon.png",
                            typeof (Texture2D));
            }
            return _openIcon;
        }
    }

    public static Texture2D saveIcon
    {
        get
        {
            if (_saveIcon == null)
            {
                _saveIcon =
                    (Texture2D)
                        AssetDatabase.LoadAssetAtPath("Assets/Infinity Code/Real World Terrain/Icons/SaveIcon.png",
                            typeof (Texture2D));
            }
            return _saveIcon;
        }
    }
}