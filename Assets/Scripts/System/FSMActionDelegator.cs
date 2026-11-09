using System.Collections.Generic;
using Assets.Scripts.Camera;
using Assets.Scripts.Entities;
using UnityEngine;

namespace Assets.Scripts.System
{
    public class FSMActionDelegator
    {
        private static readonly HashSet<string> WarnedKeys = new HashSet<string>();

        // Unimplemented script commands can run every frame, so only report each one once.
        private static void WarnOnce(string key, string message)
        {
            if (WarnedKeys.Add(key))
            {
                Debug.LogWarning(message);
            }
        }

        // Forget what was already reported, so each level logs its own first occurrence of every problem.
        public static void ResetWarnings()
        {
            WarnedKeys.Clear();
        }

        // A script action that throws (for example because its car was already destroyed) must not stop
        // every script from running, so the runner catches it and reports it here, with the full exception
        // the first time per action in each level.
        public static void ReportActionError(string actionName, global::System.Exception ex)
        {
            if (WarnedKeys.Add("error:" + actionName))
            {
                Debug.LogError("FSM action '" + actionName + "' failed (further failures of this action are not logged this level).");
                Debug.LogException(ex);
            }
        }

        // Scripts may name an entity that was never spawned (for example a turret the vehicle parser cannot load yet).
        private static Car CarOf(FSMEntity entity)
        {
            return entity != null && entity.Object != null ? entity.Object.GetComponent<Car>() : null;
        }

        private static void WarnMissingEntity(string actionName, int entityIndex)
        {
            WarnOnce("missing:" + actionName + ":" + entityIndex, "FSM action '" + actionName + "' skipped: entity " + entityIndex + " was not spawned.");
        }

        private void LogUnhandledEntity(string actionName, int entityIndex, FSMEntity entity, StackMachine machine)
        {
            WarnOnce("entity:" + actionName + ":" + entity.Value, "FSM action '" + actionName + "' not implemented for entity " + entityIndex + " (" + entity.Value + ") @ " + (machine.IP - 1));
        }

        public int DoAction(string actionName, StackMachine machine, FSMRunner fsmRunner)
        {
            global::System.Collections.Generic.Queue<IntRef> args = machine.ArgumentQueue;
            switch (actionName)
            {
                case "null":
                    // Do nothing?
                    break;
                case "true":
                    return 1;
                case "inc":
                    {
                        IntRef arg = args.Dequeue();
                        ++arg.Value;
                    }
                    break;
                case "dec":
                    {
                        IntRef arg = args.Dequeue();
                        --arg.Value;
                    }
                    break;
                case "set":
                    {
                        IntRef arg = args.Dequeue();
                        IntRef val = args.Dequeue();
                        arg.Value = val.Value;
                    }
                    break;
                case "isGreater":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool greater = val.Value > number.Value;
                    return greater ? 1 : 0;
                }
                case "isLesser":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool lesser = val.Value < number.Value;
                    return lesser ? 1 : 0;
                }
                case "isEqual":
                {
                        IntRef val = args.Dequeue();
                        IntRef number = args.Dequeue();

                    bool equal = val.Value == number.Value;
                    return equal ? 1 : 0;
                }
                case "pushCam":
                    CameraManager.Instance.PushCamera();
                    break;
                case "popCam":
                    CameraManager.Instance.PopCamera();
                    break;
                case "timeGreater":
                    IntRef timerNo = args.Dequeue();
                    IntRef seconds = args.Dequeue();

                    float secondsElapsed = Time.unscaledTime - fsmRunner.Timers[timerNo.Value];
                    return secondsElapsed >= seconds.Value ? 1 : 0;
                case "isKeypress":
                    return DevInput.GetKeyDown(KeyCode.Space) ? 1 : 0;
                case "camObjDir":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        var camera = CameraManager.Instance.ActiveCamera;
                        IntRef whichEntity = args.Dequeue();
                        FSMEntity origoEntity = fsmRunner.FSM.EntityTable[whichEntity.Value];
                        GameObject entity = origoEntity.Object;

                        Vector3 relativePos = new Vector3(args.Dequeue().Value, args.Dequeue().Value, args.Dequeue().Value) / 100.0f;

                        int yaw = args.Dequeue().Value;
                        int roll = args.Dequeue().Value;
                        int pitch = args.Dequeue().Value;

                        Vector3 rotation = new Vector3(yaw, pitch, roll) / 100.0f;

                        Vector3 newPos = entity.transform.position + (entity.transform.rotation * relativePos);

                        if (newPos.y < entity.transform.position.y + 1)
                            newPos.y = entity.transform.position.y + 1;

                        camera.transform.position = newPos;
                        camera.transform.rotation = entity.transform.rotation * Quaternion.Euler(rotation);
                    }
                    break;
                case "camPosObj":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        var camera = CameraManager.Instance.ActiveCamera;
                        int pathIndex = args.Dequeue().Value;
                        int height = args.Dequeue().Value;
                        int watchTarget = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        camera.transform.position = nodePos;

                        GameObject entity = fsmRunner.FSM.EntityTable[watchTarget].Object;
                        if (entity == null) { WarnMissingEntity(actionName, watchTarget); break; }
                        camera.transform.LookAt(entity.transform, Vector3.up);
                    }
                    break;
                case "camObjObj":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        int objectIndex = args.Dequeue().Value;
                        float xPos = args.Dequeue().Value;
                        float yPos = args.Dequeue().Value;
                        float zPos = args.Dequeue().Value;
                        int watchTarget = args.Dequeue().Value;
                        
                        FSMEntity anchorEntity = fsmRunner.FSM.EntityTable[objectIndex];
                        FSMEntity targetEntity = fsmRunner.FSM.EntityTable[watchTarget];

                        if (anchorEntity.Object == null || targetEntity.Object == null) { WarnMissingEntity(actionName, objectIndex); break; }
                        var camera = CameraManager.Instance.ActiveCamera;
                        camera.transform.SetParent(anchorEntity.Object.transform);
                        camera.transform.localPosition = new Vector3(xPos * 0.01f, zPos * 0.01f, yPos * 0.01f);
                        camera.transform.LookAt(targetEntity.Object.transform, Vector3.up);
                    }
                    break;
                case "camTransObj":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }
                        
                        var camera = CameraManager.Instance.ActiveCamera;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;
                        float height = args.Dequeue().Value * 0.01f;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];
                        
                        if (args.Count == 0)
                        {
                            Vector3 nodePos = path.GetWorldPosition(0);
                            nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height;
                            camera.transform.position = nodePos;
                            break;
                        }

                        int watchTarget = args.Dequeue().Value;
                        GameObject entity = fsmRunner.FSM.EntityTable[watchTarget].Object;
                        if (entity == null) { WarnMissingEntity(actionName, watchTarget); break; }
                        camera.transform.LookAt(entity.transform, Vector3.up);
                        camera.SetTargetPath(path, targetSpeed, height);
                        
                        
                    }
                    break;
                case "camIsArrived":
                    {
                        if (CameraManager.Instance.IsMainCameraActive)
                        {
                            break;
                        }

                        var camera = CameraManager.Instance.ActiveCamera;
                        if (camera.Arrived)
                        {
                            camera.Arrived = false;
                            return 1;
                        }
                        return 0;
                    }
                case "goto":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "evade":
                case "race":
                    {
                        // In i76.exe evade (command 0x18) and race (0x1a) set the vehicle's path and speed exactly like goto and
                        // take a fourth argument, the other entity involved, then issue their own command. Open76 only does the
                        // path and speed part; the evasion and racing tactics themselves are not implemented.
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;
                        args.Dequeue();

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            WarnOnce("partial:" + actionName, "FSM action '" + actionName + "' follows its path at its speed only; its own tactics are not implemented.");
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithinNav":
                    {
                        int pathIndex = args.Dequeue().Value;
                        int entityIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                            
                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            bool within = car.IsWithinNav(path, distance);
                            return within ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithinSqNav":
                    {
                        int pathIndex = args.Dequeue().Value;
                        int entityIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            bool within = car.IsWithinNav(path, (int)Mathf.Sqrt(distance));
                            return within ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "follow":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int targetIndex = args.Dequeue().Value;
                        int unk1 = args.Dequeue().Value;
                        int unk2 = args.Dequeue().Value;
                        int xOffset = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity targetEntity = fsmRunner.FSM.EntityTable[targetIndex];

                        Car car = CarOf(entity);
                        Car targetCar = CarOf(targetEntity);
                        if (car != null && targetCar != null)
                        {
                            car.SetFollowTarget(targetCar, xOffset, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isAtFollow":
                    {
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            bool atFollow = car.AtFollowTarget();
                            return atFollow ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "teleport":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;
                        int height = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Vector3 nodePos = path.GetWorldPosition(0);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        if (entity.Object == null) { WarnMissingEntity(actionName, entityIndex); break; }
                        entity.Object.transform.position = nodePos;

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.SetSpeed(targetSpeed);
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "teleportOffset":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int pathIndex = args.Dequeue().Value;
                        int targetSpeed = args.Dequeue().Value;
                        int height = args.Dequeue().Value;
                        var offsetX = args.Dequeue().Value;
                        var offsetZ = args.Dequeue().Value;

                        FSMPath path = fsmRunner.FSM.Paths[pathIndex];

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Vector3 nodePos = path.GetWorldPosition(0) + new Vector3(offsetX, 0, offsetZ);
                        nodePos.y = Utils.GroundHeightAtPoint(nodePos.x, nodePos.z) + height * 0.01f;
                        if (entity.Object == null) { WarnMissingEntity(actionName, entityIndex); break; }
                        entity.Object.transform.position = nodePos;

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.SetSpeed(targetSpeed);
                            car.SetTargetPath(path, targetSpeed);
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isArrived":
                    {
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity origoEntity = fsmRunner.FSM.EntityTable[entityIndex];
                        GameObject entity = origoEntity.Object;

                        Car car = entity.GetComponent<Car>();
                        if (car != null)
                        {
                            if (car.Arrived)
                            {
                                car.Arrived = false;
                                return 1;
                            }

                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, origoEntity, machine);
                    }
                    break;
                case "sit":
                    {
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.Sit();
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setAvoid":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int avoidIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity avoidEntity = fsmRunner.FSM.EntityTable[avoidIndex];

                        Car car = CarOf(entity);
                        WorldEntity target = avoidEntity.WorldEntity;
                        if (car != null && target != null)
                        {
                            // TODO: Figure out 'avoid' logic - don't path near object?
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setMaxAttackers":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int maxAttackers = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];

                        if (entity.WorldEntity != null)
                        {
                            entity.WorldEntity.MaxAttackers = maxAttackers;
                            break;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setSkill":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int skill1 = args.Dequeue().Value;
                        int skill2 = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.Skill1 = skill1;
                            car.Skill2 = skill2;
                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "setAgg":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int aggressionValue = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            car.Aggressiveness = aggressionValue;
                            return 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isAttacked":
                    {
                        int entityIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            return car.Attacked ? 1 : 0;
                        }

                        Turret turret = entity.Object != null ? entity.Object.GetComponent<Turret>() : null;
                        if (turret != null)
                        {
                            return turret.Attacked ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isDead":
                    {
                        int entityIndex = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.WorldEntity != null)
                        {
                            return entity.WorldEntity.Alive ? 0 : 1;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "isWithin":
                    {
                        int entityIndex = args.Dequeue().Value;
                        int targetIndex = args.Dequeue().Value;
                        int distance = args.Dequeue().Value;

                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        FSMEntity target = fsmRunner.FSM.EntityTable[targetIndex];

                        if (entity.Object == null || target.Object == null) { WarnMissingEntity(actionName, entityIndex); return 0; }
                        bool within = Vector3.Distance(entity.Object.transform.position, target.Object.transform.position) < distance;
                        return within ? 1 : 0;
                    }
                case "cbFromPrior":
                    {
                        int soundId = args.Dequeue().Value;
                        int owner = args.Dequeue().Value;
                        int queueFlag = args.Dequeue().Value;
                        QueueRadio(fsmRunner, soundId, queueFlag, owner);
                    }
                    break;
                case "cbPrior":
                    {
                        int soundId = args.Dequeue().Value;
                        int queueFlag = args.Dequeue().Value;
                        QueueRadio(fsmRunner, soundId, queueFlag, -1);
                    }
                    break;
                case "rand":
                    {
                        IntRef arg = args.Dequeue();
                        IntRef val = args.Dequeue();
                        arg.Value = Random.Range(0, val.Value);
                    }
                    break;
                case "stopCB":
                    RadioManager.Instance.Stop();
                    break;
                case "isCBEmpty":
                    {
                        return RadioManager.Instance.IsQueueEmpty() ? 1 : 0;
                    }
                case "startTimer":
                    int timerIndex = args.Dequeue().Value;
                    fsmRunner.Timers[timerIndex] = Time.unscaledTime;
                    break;
                case "failAllObj":
                    {
                        // Fails the mission after a delay with failure message number n (i76.exe: same family as failAll
                        // and successAll, which set the same deadline and the mission result).
                        int delay = args.Dequeue().Value;
                        int messageNumber = args.Dequeue().Value;
                        fsmRunner.FailMission(delay, messageNumber);
                    }
                    break;
                // The four actions below follow what i76.exe does (see Projects/Dev/Interstate 76 Gaps in the vault).
                case "destroy":
                    {
                        // Kills a car or a structure. i76.exe logs an error when the target is already dead.
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.Object == null)
                        {
                            WarnMissingEntity(actionName, entityIndex);
                            break;
                        }

                        Car car = CarOf(entity);
                        Turret turret = entity.Object.GetComponent<Turret>();
                        Building building = entity.Object.GetComponent<Building>();
                        if (car != null)
                        {
                            car.Kill();
                        }
                        else if (turret != null)
                        {
                            turret.Kill();
                        }
                        else if (building != null)
                        {
                            building.Kill();
                        }
                        else
                        {
                            LogUnhandledEntity(actionName, entityIndex, entity, machine);
                        }
                    }
                    break;
                case "isShot":
                    {
                        // True once when the vehicle was shot this tick or the previous one, then cleared.
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.Object == null)
                        {
                            WarnMissingEntity(actionName, entityIndex);
                            return 0;
                        }

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            return car.ConsumeShot() ? 1 : 0;
                        }

                        Turret turret = entity.Object.GetComponent<Turret>();
                        if (turret != null)
                        {
                            return turret.ConsumeShot() ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                case "allEnemyDead":
                    {
                        // True when every vehicle on every team other than the entity's own (teams 1 to 6) is dead.
                        int entityIndex = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.Object == null)
                        {
                            WarnMissingEntity(actionName, entityIndex);
                            return 0;
                        }

                        Car self = CarOf(entity);
                        Turret selfTurret = entity.Object.GetComponent<Turret>();
                        if (self == null && selfTurret == null)
                        {
                            LogUnhandledEntity(actionName, entityIndex, entity, machine);
                            break;
                        }

                        int team = self != null ? self.TeamId : selfTurret.TeamId;
                        foreach (Car other in EntityManager.Instance.Cars)
                        {
                            if (other.TeamId >= 1 && other.TeamId <= 6 && other.TeamId != team && other.Alive)
                            {
                                return 0;
                            }
                        }

                        return 1;
                    }
                case "hpLesser":
                    {
                        // True when the entity's health (0 to 100) is below the threshold. The second argument is not used by i76.exe.
                        int entityIndex = args.Dequeue().Value;
                        args.Dequeue();
                        int threshold = args.Dequeue().Value;
                        FSMEntity entity = fsmRunner.FSM.EntityTable[entityIndex];
                        if (entity.Object == null)
                        {
                            WarnMissingEntity(actionName, entityIndex);
                            return 0;
                        }

                        Car car = CarOf(entity);
                        if (car != null)
                        {
                            return car.HealthPercent < threshold ? 1 : 0;
                        }

                        Turret turret = entity.Object.GetComponent<Turret>();
                        if (turret != null)
                        {
                            return turret.HealthPercent < threshold ? 1 : 0;
                        }

                        Building building = entity.Object.GetComponent<Building>();
                        if (building != null)
                        {
                            return building.HealthPercent < threshold ? 1 : 0;
                        }

                        LogUnhandledEntity(actionName, entityIndex, entity, machine);
                    }
                    break;
                default:
                    WarnOnce("action:" + actionName, "FSM action not implemented: " + actionName + " @ " + (machine.IP-1));
                    break;
            }

            return 0;
        }

        private void QueueRadio(FSMRunner fsmRunner, int soundId, int queueFlag, int owner)
        {
            bool endOfQueue;
            if (queueFlag == 1)
            {
                endOfQueue = false;
            }
            else if (queueFlag == 3)
            {
                endOfQueue = true;
            }
            else
            {
                endOfQueue = true;
                WarnOnce("cbPrior:" + queueFlag, "Unknown value in cbPrior: " + queueFlag);
            }

            string soundName = fsmRunner.FSM.SoundClipTable[soundId];
            RadioManager.Instance.QueueRadioMessage(soundName, endOfQueue, owner);
        }
    }
}
