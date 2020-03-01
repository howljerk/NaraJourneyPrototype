using UnityEngine;

public static class CollisionUtils
{
    public class CollideResults
    {
        public RectTransform.Axis m_PushOutAxis;
        public float m_PushOut;
    }

    public static bool GetDoesCollide(Vector3 boundsACenter, Bounds boundsA, Vector3 boundsBCenter, Bounds boundsB, out CollideResults resultsOut, bool clampCheckTo2D = true)
    {
        resultsOut = new CollideResults();

        if (clampCheckTo2D)
            boundsACenter = new Vector3(boundsACenter.x,
                boundsACenter.y,
                boundsBCenter.z);

        boundsA.center = boundsACenter;
        boundsB.center = boundsBCenter;

        bool doesCollide = boundsA.Intersects(boundsB);

        if (doesCollide)
        {
            float xPushOut = 0f;
            if (boundsA.max.x > boundsB.min.x && boundsA.max.x < boundsB.max.x)
                xPushOut = -(boundsA.max.x - boundsB.min.x);
            if (boundsA.min.x < boundsB.max.x && boundsA.max.x > boundsB.max.x)
                xPushOut = boundsB.max.x - boundsA.min.x;

            float yPushOut = 0f;
            if (boundsA.max.y > boundsB.min.y && boundsA.min.y < boundsB.min.y)
                yPushOut = -(boundsA.max.y - boundsB.min.y);
            if (boundsA.min.y < boundsB.max.y && boundsA.max.y > boundsB.max.y)
                yPushOut = boundsB.max.y - boundsA.min.y;

            float absXPushOut = Mathf.Abs(xPushOut);
            float absYPushOut = Mathf.Abs(yPushOut);

            resultsOut.m_PushOutAxis = absYPushOut < absXPushOut && absYPushOut > 0 ?
                RectTransform.Axis.Vertical :
                RectTransform.Axis.Horizontal;

            resultsOut.m_PushOut = resultsOut.m_PushOutAxis == RectTransform.Axis.Horizontal ?
                xPushOut :
                yPushOut;

            if (Mathf.Abs(resultsOut.m_PushOut) > 0)
                resultsOut.m_PushOut += Mathf.Sign(resultsOut.m_PushOut) * .01f;
        }

        return doesCollide;
    }
}
