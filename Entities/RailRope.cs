﻿using Celeste.Mod.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Verillia.Utils.Meta;

namespace Celeste.Mod.Verillia.Utils.Entities
{
    //Thanks to Viv for handling the catenary calculus stuff!
    [CustomEntity("Verillia/Utils/RailBooster/Rail")]
    public class RailRope : Entity
    {
        //Rope details
        public RailBooster endA;
        public int indexA
        {
            get
            {
                return endA.Rails.IndexOf(this);
            }
            private set { }
        }
        public RailBooster endB;
        public int indexB
        {
            get
            {
                return endB.Rails.IndexOf(this);
            }
            private set { }
        }
        public Vector2[] points { get; private set; }

        //Priority allows default thing
        public readonly int Priority = 0;

        //Rendered Wobble
        private SineWave Wobble;
        private const float MinWobbleFrequency = 1f;
        private const float MaxWobbleFrequency = 1.5f;

        //Rope specs
        public const string RopeColor = "FFFFFF";
        public const float RopeThickness = 4f;

        // curve is defined by y = a*cosh((x-p) / a) + q
        public double a, p, q;

        const double IntervalStep = 1;
        const float Precision = 0.0001f;
        const int MinPoints = 8;
        const int PointDistance = 16;
        const float DiversionDistance = 4;

        public int xStart, xEnd;

        public RailRope(Vector2 position, EntityData data)
        {
            //Meta stuff
            Depth = VerilliaUtilsDepths.RailBoosterRope;
            Priority = data.Int("priority", 0);
            Position = position;
            //Set up the catenary
            Vector2 p0 = ((position.X < data.Nodes[0].X) ? position : data.Nodes[0]);
            Vector2 p1 = ((position.X < data.Nodes[0].X) ? data.Nodes[0] : position);
            float length = Math.Max(data.Float("length",0), (p1-p0).Length());
            calcCatenary(p0, p1, length);
            //Define vertexes (yes, I call them points)
            points = new Vector2[Math.Max(MinPoints, (int)Math.Ceiling(length/PointDistance))];
            for(int index = 0; index < points.Length-1; index++) // if you are wondering, the last point is p1
            {
                points[index] = generatePointsAtDistanceAlongCurve(p0, length * (index / points.Length-1)); // seperate the points by equal gaps
            }
            points[^1] = p1;
            //Initialize the wobble
            //Not sure how to make to equal catenaries seamlessly sync
            //Especially across two levels
            //This is the best I could think of
            Calc.PushRandom((p0*length).GetHashCode());
            Add(Wobble = new SineWave(Calc.Random.Range(MinWobbleFrequency, MaxWobbleFrequency)));
            Wobble.Randomize();
        }

        public override void Awake(Scene scene)
        {
            // Attaching the rope, simple as that
            RailBooster referral = scene.Tracker.GetNearestEntity<RailBooster>(points[0]);
            if (referral != null && referral.Position == points[0])
                endA = referral;
            else
                endA = new RailBooster(points[0], false);
            endA.AddRail(this);
            referral = scene.Tracker.GetNearestEntity<RailBooster>(points[^1]);
            if (referral != null && referral.Position == points[^1])
                endB = referral;
            else
                endB = new RailBooster(points[^1], false);
            endB.AddRail(this);
            base.Awake(scene); 
        }

        public Vector2[] getPathFrom(Vector2 origin)
        {
            if ((origin - points[0]).LengthSquared() < (origin - points[^1]).LengthSquared())
                return points;
            return points.Reverse().ToArray();
        }

        private void calcCatenary(Vector2 p0, Vector2 p1, float length)
        {
            float h = p1.X - p0.X;
            float v = p1.Y - p0.Y;
            xStart = (int)p0.X;
            xEnd = (int)p1.X;
            a = 0;
            double d = 0;
            // Get Initial value for a
            for (int _ = 0; _ < 25; _++)
            { // if your a is greater than 25, that's a different issue
                a += IntervalStep;
                d = 2 * a * approxSinh(h / (2 * a));
                if (Math.Pow(length, 2) - Math.Pow(v, 2) < d * d)
                    break;
            }
            // Bisect the check for a
            double a_prev = a - IntervalStep;
            double a_next = a;
            for (int _ = 0; _ < 15; _++)
            { // this gets down to 1 / 2^15, if you've somehow missed it your a is most likely wayyyyyyyyyyyyyy too high (that's also why we need double precision)
                a = (a_prev + a_next) / 2f;
                d = 2 * a * approxSinh(h / (2 * a));
                if (Math.Pow(length, 2) - Math.Pow(v, 2) < d * d)
                    a_prev = a;
                else
                    a_next = a;
                if (a_next - a_prev > Precision)
                    break;
            }
            a = a = (a_prev + a_next) / 2f;

            p = 1 + p0.X + p1.X - a * Math.Log((length + v) / (length - v));
            q = 1 + p1.Y + p0.Y - length / Math.Tanh(h / (2 * a));
            p /= 2; q /= 2;
        }

        public float GetSlopeAt(float x)
        {
            return (float)(a * approxSinh(((double)x - p) / a));
        }

        private Vector2 generatePointsAtDistanceAlongCurve(Vector2 initialPoint, float distance)
        {
            Vector2 newPoint = new Vector2(0, 0);
            newPoint.X = (float)(p - a * Math.Asinh(approxSinh((p - initialPoint.X) / a) - distance / a)); // a is the "Curvature" metric, p is X value of the minimum of the catenary (the lowest point)
            newPoint.Y = (float)(a * approxCosh((newPoint.X - p) / a) + q); // q is the Y-value of the minimum of the catenary (The lowest point)
            return newPoint;
        }


        public override void Render()
        {
            // draw the rope (obviously)
            for (int index = 0; index < (points.Length - 2); index++)
            {
                Draw.Line(
                    points[index],
                    points[index + 1],
                    Calc.HexToColor(RopeColor),
                    RopeThickness);
            }
            base.Render();
        }

        public override void Update()
        {
            base.Update();
        }

        // Unroll into 3 sets of taylor expansion, becomes miniscule at around here. I think you remove 6th/7th power if performance is bad
        static public double approxCosh(double v) => 1 + 0.5 * Math.Pow(v, 2) + 0.0833333333 * Math.Pow(v, 4) + 0.00138888889 * Math.Pow(v, 6);
        static public double approxSinh(double v) => v + 0.166666666666 * Math.Pow(v, 3) + 0.00833333333 * Math.Pow(v, 5) + 0.000198412698 * Math.Pow(v, 7);
        

    }
}
