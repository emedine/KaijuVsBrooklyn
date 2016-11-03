/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class RealWorldTerrainImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("RWT_Result"))
        {
            Match match = Regex.Match(assetPath, @"r(\d+)x(\d+)");
            if (match.Success)
            {
                int width = int.Parse(match.Groups[1].Value);
                int height = int.Parse(match.Groups[2].Value);
                TextureImporter textureImporter = assetImporter as TextureImporter;
                textureImporter.isReadable = true;
                textureImporter.maxTextureSize = Mathf.Max(width, height);
            }
        }
    }
}