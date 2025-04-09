using System;
using UnityEngine;

namespace AnythingWorld.Voice
{
    public static class CommandSchema
    {

        public static RelativeLocationSchema ParseRelativeLocation(string locationString)
        {
            RelativeLocationSchema locationSchema;
            Enum.TryParse<RelativeLocationSchema>(locationString, out locationSchema);
            return locationSchema;
        }
        public static ActionSchema ParseAction(string actionString)
        {
            ActionSchema actionSchema;
            Enum.TryParse<ActionSchema>(actionString, out actionSchema);
            return actionSchema;
        }
        public static HabitatSchema ParseHabitat(string habitatString)
        {
            HabitatSchema habitatSchema;
            Enum.TryParse<HabitatSchema>(habitatString, out habitatSchema);
            return habitatSchema;
        }

        public enum RelativeLocationSchema
        {
            none,
            here,
            near,
            far,
            front,
            behind,
            above,
            below,
            left,
            right
        }
        public enum ActionSchema
        {
            none,
            move_model,
            add_model,
            remove_model,
            change_habitat
        }
        public enum HabitatSchema
        {
            garden,
            urban,
            rural,
            grass,
            swamp,
            river,
            pond,
            lake,
            jungle,
            icescape,
            sea,
            farm,
            desert,
            city,
            beach,
            forest,
            cave,
            underwater,
            magical,
            grassland,
            mountain,
            space,
            savannah,
            testing,
        }

    }
}
