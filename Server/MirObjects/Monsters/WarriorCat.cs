using System;
using System.Collections.Generic;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    class WarriorCat : MonsterObject
    {
        protected internal WarriorCat(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 3 || y > 3) return false;


            return (x <= 1 && y <= 1) || (x == y || x % 2 == y % 2);
        }

        protected override void Attack()
        {

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            LineAttack(3);


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
            ShockTime = 0;


        }

        private void LineAttack(int distance)
        {

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (target == Target.CurrentLocation)
                {
                    if (Target.Attacked(this, damage*3, DefenceType.MACAgility) > 0 && Envir.Random.Next(8) == 0)
                    {
                        if (Envir.Random.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                        {
                            Target.ApplyPoison(new Poison { Owner = this, Duration = 15, PType = PoisonType.Red, Value = GetAttackPower(MinSC, MaxSC), TickSpeed = 2000 }, this);
                        }
                    }
                }
                else
                {
                    List<MapObject> targets = FindAllTargets(3, CurrentLocation, false);
                    if (targets.Count == 0) return;

                    for (int o = 0; o < targets.Count; o++)
                    {
                        Target = targets[o];
                            if (!targets[o].IsAttackTarget(this)) continue;

                            if (targets[o].Attacked(this, damage, DefenceType.MACAgility) > 0 && Envir.Random.Next(8) == 0)
                            {
                                if (Envir.Random.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                                {
                                    Target.ApplyPoison(new Poison { Owner = this, Duration = 15, PType = PoisonType.Red, Value = GetAttackPower(MinSC, MaxSC), TickSpeed = 2000 }, this);
                                }
                            }

                    }
                        break;
                    }
                }
        }
    }
}
