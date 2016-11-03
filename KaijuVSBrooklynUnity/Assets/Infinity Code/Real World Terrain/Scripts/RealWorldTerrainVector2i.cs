/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using UnityEngine;

[System.Serializable]
public class RealWorldTerrainVector2i
{
    public static RealWorldTerrainVector2i one
    {
        get
        {
            return new RealWorldTerrainVector2i(1, 1);
        }
    }

    public int x;
    public int y;

    public int magnitude
    {
        get { return x * y; }
    }

    public int max
    {
        get { return Mathf.Max(x, y); }
    }

    public RealWorldTerrainVector2i(int X, int Y)
    {
        x = X;
        y = Y;
    }

    public override string ToString()
    {
        return string.Format("X: {0}, Y: {1}", x, y);
    }

    public static implicit operator Vector2(RealWorldTerrainVector2i val)
    {
        return new Vector2(val.x, val.y);
    }

    public static RealWorldTerrainVector2i operator -(RealWorldTerrainVector2i v1, RealWorldTerrainVector2i v2)
    {
        return new RealWorldTerrainVector2i(v1.x - v2.x, v1.y - v2.y);
    }
}