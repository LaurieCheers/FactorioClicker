using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FactorioClicker.UI
{
    public enum UIAnchorSide
    {
        CENTER, // centers aligned
        TOP, // beyond the top side, centers aligned (horizontally)
        BOTTOM, // beyond the bottom side, centers aligned (horizontally)
        LEFT, // beyond the left side, centers aligned (vertically)
        RIGHT, // beyond the right side, centers aligned (vertically)
        INSIDE_TOP, // top edges aligned, centers aligned (horizontally)
        INSIDE_BOTTOM, // bottom edges aligned, centers aligned (horizontally)
        INSIDE_LEFT, // left edges aligned, centers aligned (vertically)
        INSIDE_RIGHT, // right edges aligned, centers aligned (vertically)
        TOP_INSIDE_LEFT, // beyond the top side, left edges aligned
        TOP_INSIDE_RIGHT, // beyond the top side, right edges aligned
        BOTTOM_INSIDE_LEFT, // beyond the bottom side, left edges aligned
        BOTTOM_INSIDE_RIGHT, // beyond the bottom side, right edges aligned
        LEFT_INSIDE_TOP, // beyond the left side, top edges aligned
        LEFT_INSIDE_BOTTOM, // beyond the left side, bottom edges aligned
        RIGHT_INSIDE_TOP, // beyond the right side, top edges aligned
        RIGHT_INSIDE_BOTTOM, // beyond the right side, bottom edges aligned
    }
}
