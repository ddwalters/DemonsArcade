using System;

[Serializable]
public class SizeInt
{
    public int width;

    public int height;

    public SizeInt(int width, int height)
    {
        this.width = width;
        this.height = height;
    }
}