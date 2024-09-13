using System;
using System.Collections.Generic;
using System.Text;

namespace PerfectlyNormalUnity.FollowDirectionPosition
{
    public enum FollowDirectionType
    {
        //------ this is an attraction force

        /// <summary>
        /// The force is along the direction vector
        /// </summary>
        Attract_Direction,

        //------ everything below is a drag force

        /// <summary>
        /// Drag is applied to the entire velocity
        /// </summary>
        Drag_Velocity_Any,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point
        /// </summary>
        Drag_Velocity_Along,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point.
        /// But only if that velocity is toward the chase point
        /// </summary>
        Drag_Velocity_AlongIfVelocityToward,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is along the direction to the chase point.
        /// But only if that velocity is away from chase point
        /// </summary>
        Drag_Velocity_AlongIfVelocityAway,
        /// <summary>
        /// Drag is only applied along the part of the velocity that is othrogonal to the direction to the chase point
        /// </summary>
        Drag_Velocity_Orth
    }
}
