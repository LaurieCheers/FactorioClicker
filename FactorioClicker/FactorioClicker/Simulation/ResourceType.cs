using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.UI;
using Microsoft.Xna.Framework.Content;
using FactorioClicker.Graphics;

namespace FactorioClicker.Simulation
{
    public class ResourceType
    {
        public LayeredImage image;
        public String name;
        public int maxAmount;
        public HashSet<String> subtypes;

        public ResourceType(JSONTable template, ContentManager content)
        {
            name = template.getString("name");
            image = new LayeredImage(template.getJSON("image"), content);
            maxAmount = template.getInt("maxAmount", 100);
            subtypes = new HashSet<string>();

            JSONArray subtypeStrings = template.getArray("subtypes", null);
            if (subtypeStrings != null)
            {
                for (int Idx = 0; Idx < subtypeStrings.Length; ++Idx)
                {
                    subtypes.Add(subtypeStrings.getString(Idx));
                }
            }
        }
    }
}
