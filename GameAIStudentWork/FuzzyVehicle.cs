using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameAI;

// All the Fuzz
using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;

namespace GameAICourse
{

    public class FuzzyVehicle : AIVehicle
    {

        // TODO create some Fuzzy Set enumeration types, and member variables for:
        // Fuzzy Sets (input and output), one or more Fuzzy Value Sets, and Fuzzy
        // Rule Sets for each output.
        // Also, create some methods to instantiate each of the member variables

        // Here are some basic examples to get you started
        enum FzOutputThrottle {Brake, Coast, Accelerate }
        enum FzOutputWheel { TurnLeft, Straight, TurnRight }

        enum FzInputSpeed { Slow, Medium, Fast }

        enum FzInputNextPointHeading { Left, StraightAhead, Right }

        FuzzySet<FzInputSpeed> fzSpeedSet;
        FuzzySet<FzInputNextPointHeading> fzNextPointHeadingSet;

        FuzzySet<FzOutputThrottle> fzThrottleSet;
        FuzzyRuleSet<FzOutputThrottle> fzThrottleRuleSet;

        FuzzySet<FzOutputWheel> fzWheelSet;
        FuzzyRuleSet<FzOutputWheel> fzWheelRuleSet;

        FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

        // These are used for debugging (see ApplyFuzzyRules() call
        // in Update()
        FuzzyValueSet mergedThrottle = new FuzzyValueSet();
        FuzzyValueSet mergedWheel = new FuzzyValueSet();



        private FuzzySet<FzInputSpeed> GetSpeedSet()
        {
            FuzzySet<FzInputSpeed> set = new FuzzySet<FzInputSpeed>();

            var slow = new ShoulderMembershipFunction(0f, new Coords(0f, 1f), new Coords(45f, 0f), 60f);
            var medium = new TriangularMembershipFunction(new Coords(0f, 0f), new Coords(30f, 1f), new Coords(60f, 0f));
            var fast = new ShoulderMembershipFunction(0f, new Coords(50f, 0f), new Coords(60f, 1f), 60);

            set.Set(new FuzzyVariable<FzInputSpeed>(FzInputSpeed.Slow, slow));
            set.Set(new FuzzyVariable<FzInputSpeed>(FzInputSpeed.Medium, medium));
            set.Set(new FuzzyVariable<FzInputSpeed>(FzInputSpeed.Fast, fast));

            return set;
        }

        private FuzzySet<FzInputNextPointHeading> GetNextPointHeadingSet()
        {
            FuzzySet<FzInputNextPointHeading> set = new FuzzySet<FzInputNextPointHeading>();

            var left = new ShoulderMembershipFunction(-12f, new Coords(-12f, 1f), new Coords(0f, 0f), 12f);
            var straight = new TriangularMembershipFunction(new Coords(-12f, 0f), new Coords(0f, 1f), new Coords(12f, 0f));
            var right = new ShoulderMembershipFunction(-12f, new Coords(0f, 0f), new Coords(12f, 1f), 12f);

            set.Set(new FuzzyVariable<FzInputNextPointHeading>(FzInputNextPointHeading.Left, left));
            set.Set(new FuzzyVariable<FzInputNextPointHeading>(FzInputNextPointHeading.StraightAhead, straight));
            set.Set(new FuzzyVariable<FzInputNextPointHeading>(FzInputNextPointHeading.Right, right));

            return set;
        }

        private FuzzySet<FzOutputThrottle> GetThrottleSet()
        {

            FuzzySet<FzOutputThrottle> set = new FuzzySet<FzOutputThrottle>();

            var brake = new ShoulderMembershipFunction(-1f, new Coords(-1f, 1f), new Coords(0f, 0f), 1f);
            var coast = new TriangularMembershipFunction(new Coords(-1f, 0f), new Coords(0f, 1f), new Coords(1f, 0f));
            var accelerate = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(1f, 1f), 1f);

            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Brake, brake));
            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Coast, coast));
            set.Set(new FuzzyVariable<FzOutputThrottle>(FzOutputThrottle.Accelerate, accelerate));

            return set;
        }

        private FuzzySet<FzOutputWheel> GetWheelSet()
        {

            FuzzySet<FzOutputWheel> set = new FuzzySet<FzOutputWheel>();

            var left = new ShoulderMembershipFunction(-1f, new Coords(-1f, 1f), new Coords(0f, 0f), 1f);
            var straight = new TriangularMembershipFunction(new Coords(-1f, 0f), new Coords(0f, 1f), new Coords(1f, 0f));
            var right = new ShoulderMembershipFunction(-1f, new Coords(0f, 0f), new Coords(1f, 1f), 1f);

            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.TurnLeft, left));
            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.Straight, straight));
            set.Set(new FuzzyVariable<FzOutputWheel>(FzOutputWheel.TurnRight, right));

            return set;
        }


        private FuzzyRule<FzOutputThrottle>[] GetThrottleRules()
        {

            FuzzyRule<FzOutputThrottle>[] rules =
            {
                If(FzInputSpeed.Slow).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Medium).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Fast).Then(FzOutputThrottle.Coast),
            };

            return rules;
        }

        private FuzzyRule<FzOutputWheel>[] GetWheelRules()
        {

            FuzzyRule<FzOutputWheel>[] rules =
            {
                If(FzInputNextPointHeading.Left).Then(FzOutputWheel.TurnLeft),
                If(FzInputNextPointHeading.StraightAhead).Then(FzOutputWheel.Straight),
                If(FzInputNextPointHeading.Right).Then(FzOutputWheel.TurnRight),
            };

            return rules;
        }

        private FuzzyRuleSet<FzOutputThrottle> GetThrottleRuleSet(FuzzySet<FzOutputThrottle> throttle)
        {
            var rules = this.GetThrottleRules();
            return new FuzzyRuleSet<FzOutputThrottle>(throttle, rules);
        }

        private FuzzyRuleSet<FzOutputWheel> GetWheelRuleSet(FuzzySet<FzOutputWheel> wheel)
        {
            var rules = this.GetWheelRules();
            return new FuzzyRuleSet<FzOutputWheel>(wheel, rules);
        }


        protected override void Awake()
        {
            base.Awake();

            StudentName = "Matthew Brown";

            // Only the AI can control. No humans allowed!
            IsPlayer = false;

        }

        protected override void Start()
        {
            base.Start();

            // TODO: You can initialize a bunch of Fuzzy stuff here
            fzSpeedSet = this.GetSpeedSet();
            fzNextPointHeadingSet = this.GetNextPointHeadingSet();

            fzThrottleSet = this.GetThrottleSet();
            fzThrottleRuleSet = this.GetThrottleRuleSet(fzThrottleSet);

            fzWheelSet = this.GetWheelSet();
            fzWheelRuleSet = this.GetWheelRuleSet(fzWheelSet);
        }

        System.Text.StringBuilder strBldr = new System.Text.StringBuilder();

        override protected void Update()
        {

            // TODO Do all your Fuzzy stuff here and then
            // pass your fuzzy rule sets to ApplyFuzzyRules()
            
            // Remove these once you get your fuzzy rules working.
            // You can leave one hardcoded while you work on the other.
            // Both steering and throttle must be implemented with variable
            // control and not fixed/hardcoded!

            //HardCodeSteering(0f);
            //HardCodeThrottle(1f);
            
            // Simple example of fuzzification of vehicle state
            // The Speed is fuzzified and stored in fzInputValueSet
            fzSpeedSet.Evaluate(Speed, fzInputValueSet);

            Vector3 futurePoint = pathTracker.pathCreator.path.GetPointAtDistance(pathTracker.distanceTravelled + 5f);

            Vector3 vehicleForward = transform.forward;
            Vector3 directionToFuturePoint = (futurePoint - transform.position).normalized;
            float headingAngleDifference = Vector3.SignedAngle(vehicleForward, directionToFuturePoint, Vector3.up);

            fzNextPointHeadingSet.Evaluate(headingAngleDifference, fzInputValueSet);

            // ApplyFuzzyRules evaluates your rules and assigns Thottle and Steering accordingly
            // Also, some intermediate values are passed back for debugging purposes
            // Throttle = someValue; //[-1f, 1f] -1 is full brake, 0 is neutral, 1 is full throttle
            // Steering = someValue; // [-1f, 1f] -1 if full left, 0 is neutral, 1 is full right

            ApplyFuzzyRules<FzOutputThrottle, FzOutputWheel>(
                fzThrottleRuleSet,
                fzWheelRuleSet,
                fzInputValueSet,
                // access to intermediate state for debugging
                out var throttleRuleOutput,
                out var wheelRuleOutput,
                ref mergedThrottle,
                ref mergedWheel
                );

            
            // Use vizText for debugging output
            // You might also use Debug.DrawLine() to draw vectors on Scene view
            if (vizText != null)
            {
                strBldr.Clear();

                strBldr.AppendLine($"Demo Output");
                strBldr.AppendLine($"Comment out before submission");

                // You will probably want to selectively enable/disable printing
                // of certain fuzzy states or rules

                AIVehicle.DiagnosticPrintFuzzyValueSet<FzInputSpeed>(fzInputValueSet, strBldr);
  
                AIVehicle.DiagnosticPrintRuleSet<FzOutputThrottle>(fzThrottleRuleSet, throttleRuleOutput, strBldr);
                AIVehicle.DiagnosticPrintRuleSet<FzOutputWheel>(fzWheelRuleSet, wheelRuleOutput, strBldr);

                vizText.text = strBldr.ToString();
            }

            // recommend you keep the base Update call at the end, after all your FuzzyVehicle code so that
            // control inputs can be processed properly (e.g. Throttle, Steering)
            base.Update();
        }

    }
}
