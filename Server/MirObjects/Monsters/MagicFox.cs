using System.Collections.Generic;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class MagicFox : MonsterObject
    {
        public long FearTime, TeleportTime;
        public byte AttackRange = 9;
        private bool MassAttack;
        public override MapObject Target
        {
            get { return _target; }
            set
            {
                if (_target == value) return;
                _target = value;
            }
        }

        protected internal MagicFox(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void CompleteAttack(IList<object> data)
        {
            if (Target == null) return;

            List<MapObject> targets = MassAttack ? FindAllTargets(9, CurrentLocation) : FindAllTargets(3, Target.CurrentLocation);
            if (targets.Count == 0) return;

            for (int i = 0; i < targets.Count; i++)
            {
                Target = targets[i];
                Attack();
            }
        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange() && (Envir.Time < FearTime))
            {
                if (Functions.InRange(CurrentLocation, Target.CurrentLocation, 1) && Envir.Time > TeleportTime && Envir.Random.Next(8) == 0)
                {
                    TeleportTime = Envir.Time + 5000;
                    TeleportRandom(40, 4);
                    return;
                }
                else
                    {
                        MassAttack = true;
                        Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
                        Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID });
                        int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500; //50 MS per Step
                        Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                        ActionList.Add(new DelayedAction(DelayedType.Damage, Envir.Time + delay));
                        ActionList.Add(new DelayedAction(DelayedType.Damage, Envir.Time + 500));
                    }
                ActionTime = Envir.Time + 300;
                AttackTime = Envir.Time + AttackSpeed;
            }

            FearTime = Envir.Time + 1000;

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            int dist = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (dist >= AttackRange)
                MoveTo(Target.CurrentLocation);
            else
            {
                MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

                if (Walk(dir)) return;

                switch (Envir.Random.Next(2)) //No favour
                {
                    case 0:
                        for (int i = 0; i < 10; i++)
                        {
                            dir = Functions.NextDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                    default:
                        for (int i = 0; i < 10; i++)
                        {
                            dir = Functions.PreviousDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                }

            }
        }

        protected override void Attack()
        {
            int damage = GetAttackPower(MinMC, MaxMC);
            if (damage == 0) return;

            if (Target.Attacked(this, damage, DefenceType.MAC) <= 0) return;

            if (Envir.Random.Next(5) == 0)
                Target.ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 5, TickSpeed = 1000 }, this);
        }

        public override bool TeleportRandom(int attempts, int distance, Map temp = null)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 3)
                    location = new Point(Envir.Random.Next(CurrentMap.Width), Envir.Random.Next(CurrentMap.Height));
                else
                    location = new Point(CurrentLocation.X + Envir.Random.Next(-distance, distance + 1),
                                         CurrentLocation.Y + Envir.Random.Next(-distance, distance + 1));

                if (Teleport(CurrentMap, location, true, 2)) return true;
            }

            return false;
        }
    }
}
