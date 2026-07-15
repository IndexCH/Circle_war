using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CircleWar.Tests
{
    public sealed class RoadSegmentLayoutTests
    {
        private static readonly NodeExpectation[] SpringLayout =
        {
            new NodeExpectation(0, SegmentContentType.None, "空白", ""),
            new NodeExpectation(1, SegmentContentType.None, "空白", ""),
            new NodeExpectation(2, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(3, SegmentContentType.Event, "破旧信标", "spring_salt_dust_broken_beacon"),
            new NodeExpectation(4, SegmentContentType.Npc, "玛蕾、卡洛", "spring_salt_dust_marlei_karlo_arrival_dialogue"),
            new NodeExpectation(5, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(6, SegmentContentType.None, "空白", ""),
            new NodeExpectation(7, SegmentContentType.Event, "危险预警", "spring_salt_dust_danger_warning"),
            new NodeExpectation(8, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1"),
            new NodeExpectation(9, SegmentContentType.None, "空白", ""),
            new NodeExpectation(10, SegmentContentType.Event, "封闭军用箱", "spring_salt_dust_sealed_military_crate"),
            new NodeExpectation(11, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(12, SegmentContentType.Event, "卡洛修理残骸", "spring_salt_dust_karlo_repair_wreck"),
            new NodeExpectation(13, SegmentContentType.None, "空白", ""),
            new NodeExpectation(14, SegmentContentType.Monster, "普通怪物", "salt_dust_ordinary_monster"),
            new NodeExpectation(15, SegmentContentType.Event, "玛蕾检查地表", "spring_salt_dust_marlei_surface_inspection"),
            new NodeExpectation(16, SegmentContentType.None, "空白", ""),
            new NodeExpectation(17, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(18, SegmentContentType.Event, "损坏旧枪", "spring_salt_dust_damaged_old_gun"),
            new NodeExpectation(19, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(20, SegmentContentType.None, "空白", ""),
            new NodeExpectation(21, SegmentContentType.Event, "陌生脚印", "spring_salt_dust_unfamiliar_footprints"),
            new NodeExpectation(22, SegmentContentType.None, "空白", ""),
            new NodeExpectation(23, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(24, SegmentContentType.Npc, "格拉夫、玛蕾", "spring_salt_dust_graff_marlei_conflict_dialogue"),
            new NodeExpectation(25, SegmentContentType.Event, "可拆物资", "spring_salt_dust_salvageable_materials"),
            new NodeExpectation(26, SegmentContentType.None, "空白", ""),
            new NodeExpectation(27, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(28, SegmentContentType.Event, "旧哨站记录", "spring_salt_dust_old_outpost_records"),
            new NodeExpectation(29, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1"),
            new NodeExpectation(30, SegmentContentType.Monster, "近战怪物", "salt_dust_melee_monster"),
            new NodeExpectation(31, SegmentContentType.Event, "临时补给点", "spring_salt_dust_temporary_supply_point"),
            new NodeExpectation(32, SegmentContentType.Event, "工业残骸", "spring_salt_dust_industrial_wreckage"),
            new NodeExpectation(33, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(34, SegmentContentType.None, "空白", ""),
            new NodeExpectation(35, SegmentContentType.Event, "耐盐作物种子", "spring_salt_dust_salt_tolerant_seeds"),
            new NodeExpectation(36, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(37, SegmentContentType.Monster, "精英前哨", "salt_dust_elite_outpost"),
            new NodeExpectation(38, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(39, SegmentContentType.None, "空白", "")
        };

        private static readonly NodeExpectation[] SummerLayout =
        {
            new NodeExpectation(0, SegmentContentType.None, "空白", ""),
            new NodeExpectation(1, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1"),
            new NodeExpectation(2, SegmentContentType.Event, "补给舱", "summer_low_humidity_supply_pod"),
            new NodeExpectation(3, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(4, SegmentContentType.None, "空白", ""),
            new NodeExpectation(5, SegmentContentType.Event, "通讯浮标", "summer_low_humidity_communication_buoy"),
            new NodeExpectation(6, SegmentContentType.Event, "干净水源", "summer_low_humidity_clean_water"),
            new NodeExpectation(7, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(8, SegmentContentType.Event, "巡逻无人机", "summer_low_humidity_patrol_drone"),
            new NodeExpectation(9, SegmentContentType.Npc, "监察官", "summer_low_humidity_inspector_dialogue"),
            new NodeExpectation(10, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(11, SegmentContentType.Event, "旧补给来源", "summer_low_humidity_old_supply_source"),
            new NodeExpectation(12, SegmentContentType.None, "空白", ""),
            new NodeExpectation(13, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(14, SegmentContentType.Monster, "水边怪物", "water_edge_monster"),
            new NodeExpectation(15, SegmentContentType.Event, "监察官药箱", "summer_low_humidity_inspector_medkit"),
            new NodeExpectation(16, SegmentContentType.None, "空白", ""),
            new NodeExpectation(17, SegmentContentType.Event, "旧运输轨道", "summer_low_humidity_old_transport_track"),
            new NodeExpectation(18, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(19, SegmentContentType.Resource, "强化液", "enhancement_fluid=1,food=10"),
            new NodeExpectation(20, SegmentContentType.Event, "失踪工人终端", "summer_low_humidity_missing_worker_terminal"),
            new NodeExpectation(21, SegmentContentType.None, "空白", ""),
            new NodeExpectation(22, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(23, SegmentContentType.Event, "自动门", "summer_low_humidity_automatic_door"),
            new NodeExpectation(24, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(25, SegmentContentType.None, "空白", ""),
            new NodeExpectation(26, SegmentContentType.Event, "旧货仓", "summer_low_humidity_old_warehouse"),
            new NodeExpectation(27, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1"),
            new NodeExpectation(28, SegmentContentType.Event, "格拉夫坐标", "summer_low_humidity_graff_coordinates"),
            new NodeExpectation(29, SegmentContentType.Monster, "远程水怪", "ranged_water_monster"),
            new NodeExpectation(30, SegmentContentType.None, "空白", ""),
            new NodeExpectation(31, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(32, SegmentContentType.Event, "维护讯号", "summer_low_humidity_maintenance_signal"),
            new NodeExpectation(33, SegmentContentType.Resource, "强化液", "enhancement_fluid=1,food=10"),
            new NodeExpectation(34, SegmentContentType.None, "空白", ""),
            new NodeExpectation(35, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(36, SegmentContentType.Event, "截断军方通讯", "summer_low_humidity_military_communication"),
            new NodeExpectation(37, SegmentContentType.Event, "监察官补偿", "summer_low_humidity_inspector_compensation"),
            new NodeExpectation(38, SegmentContentType.Monster, "精英怪物", "elite_swamp_monster"),
            new NodeExpectation(39, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1")
        };

        private static readonly NodeExpectation[] FallLayout =
        {
            new NodeExpectation(0, SegmentContentType.None, "空白", ""),
            new NodeExpectation(1, SegmentContentType.Event, "坠毁信号", "autumn_old_frontline_crash_signal"),
            new NodeExpectation(2, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(3, SegmentContentType.None, "空白", ""),
            new NodeExpectation(4, SegmentContentType.Resource, "芯片", "chip=1,industry=5"),
            new NodeExpectation(5, SegmentContentType.Monster, "炮塔残机", "autumn_turret_remnant"),
            new NodeExpectation(6, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(7, SegmentContentType.Event, "残骸医疗箱", "autumn_old_frontline_wreck_medkit"),
            new NodeExpectation(8, SegmentContentType.Npc, "塞维", "autumn_old_frontline_sevi_dialogue"),
            new NodeExpectation(9, SegmentContentType.Resource, "橡胶", "industry=5,rubber=1"),
            new NodeExpectation(10, SegmentContentType.None, "空白", ""),
            new NodeExpectation(11, SegmentContentType.Event, "飞行日志", "autumn_old_frontline_flight_log"),
            new NodeExpectation(12, SegmentContentType.Resource, "芯片", "chip=1,industry=5"),
            new NodeExpectation(13, SegmentContentType.Event, "燃料单元", "autumn_old_frontline_fuel_cell"),
            new NodeExpectation(14, SegmentContentType.None, "空白", ""),
            new NodeExpectation(15, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(16, SegmentContentType.Event, "识别模块", "autumn_old_frontline_identification_module"),
            new NodeExpectation(17, SegmentContentType.None, "空白", ""),
            new NodeExpectation(18, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(19, SegmentContentType.Event, "封闭军械库", "autumn_old_frontline_sealed_armory"),
            new NodeExpectation(20, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(21, SegmentContentType.Event, "临时护甲", "autumn_old_frontline_temporary_armor"),
            new NodeExpectation(22, SegmentContentType.Monster, "炮塔怪物", "autumn_turret_monster"),
            new NodeExpectation(23, SegmentContentType.Event, "旧帝国编号", "autumn_old_frontline_old_empire_number"),
            new NodeExpectation(24, SegmentContentType.Resource, "芯片", "chip=1,industry=5"),
            new NodeExpectation(25, SegmentContentType.Npc, "玛蕾、塞维、格拉夫", "autumn_old_frontline_marlei_sevi_graff_dialogue"),
            new NodeExpectation(26, SegmentContentType.None, "空白", ""),
            new NodeExpectation(27, SegmentContentType.Event, "旧导航台", "autumn_old_frontline_old_navigation_console"),
            new NodeExpectation(28, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(29, SegmentContentType.None, "空白", ""),
            new NodeExpectation(30, SegmentContentType.Event, "军用地图", "autumn_old_frontline_military_map"),
            new NodeExpectation(31, SegmentContentType.Monster, "远程怪物", "autumn_ranged_monster"),
            new NodeExpectation(32, SegmentContentType.None, "空白", ""),
            new NodeExpectation(33, SegmentContentType.Event, "关键部件", "autumn_old_frontline_key_component"),
            new NodeExpectation(34, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(35, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(36, SegmentContentType.Event, "塞维维修", "autumn_old_frontline_sevi_repair"),
            new NodeExpectation(37, SegmentContentType.Event, "逃生设备外壳", "autumn_old_frontline_escape_equipment_shell"),
            new NodeExpectation(38, SegmentContentType.Resource, "橡胶", "industry=5,rubber=1"),
            new NodeExpectation(39, SegmentContentType.Monster, "精英炮塔", "autumn_elite_turret")
        };

        private static readonly NodeExpectation[] WinterLayout =
        {
            new NodeExpectation(0, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(1, SegmentContentType.Npc, "伊莱", "winter_eli_arrival_dialogue"),
            new NodeExpectation(2, SegmentContentType.None, "空白", ""),
            new NodeExpectation(3, SegmentContentType.Event, "断裂军用电缆", "winter_broken_military_cable"),
            new NodeExpectation(4, SegmentContentType.Resource, "芯片", "chip=1,industry=5"),
            new NodeExpectation(5, SegmentContentType.Event, "废车医疗箱", "winter_abandoned_medical_kit"),
            new NodeExpectation(6, SegmentContentType.Event, "人员编制", "winter_personnel_roster"),
            new NodeExpectation(7, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(8, SegmentContentType.Event, "权限结构", "winter_authority_structure"),
            new NodeExpectation(9, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(10, SegmentContentType.None, "空白", ""),
            new NodeExpectation(11, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(12, SegmentContentType.Event, "备用能源导轨", "winter_backup_energy_rail"),
            new NodeExpectation(13, SegmentContentType.Monster, "无人机侦察", "winter_drone_scout"),
            new NodeExpectation(14, SegmentContentType.Resource, "强化液", "enhancement_fluid=1,food=10"),
            new NodeExpectation(15, SegmentContentType.Event, "招募广播", "winter_recruitment_broadcast"),
            new NodeExpectation(16, SegmentContentType.None, "空白", ""),
            new NodeExpectation(17, SegmentContentType.Event, "控制塔入口", "winter_control_tower_entrance"),
            new NodeExpectation(18, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(19, SegmentContentType.Event, "塞维急修", "winter_sevi_emergency_repair"),
            new NodeExpectation(20, SegmentContentType.Event, "旧防卫记录", "winter_old_defense_records"),
            new NodeExpectation(21, SegmentContentType.Resource, "橡胶", "industry=5,rubber=1"),
            new NodeExpectation(22, SegmentContentType.Npc, "伊莱、格拉夫", "winter_eli_graff_dialogue"),
            new NodeExpectation(23, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(24, SegmentContentType.Event, "逃生能源转接", "winter_escape_power_transfer"),
            new NodeExpectation(25, SegmentContentType.None, "空白", ""),
            new NodeExpectation(26, SegmentContentType.Resource, "芯片", "chip=1,industry=5"),
            new NodeExpectation(27, SegmentContentType.Monster, "无人机发射井", "winter_drone_launch_shaft"),
            new NodeExpectation(28, SegmentContentType.None, "空白", ""),
            new NodeExpectation(29, SegmentContentType.Resource, "压缩饼干", "compressed_biscuit=1,food=10"),
            new NodeExpectation(30, SegmentContentType.Event, "军事管制广播", "winter_military_control_broadcast"),
            new NodeExpectation(31, SegmentContentType.Resource, "芦苇花", "food=10,reed_flower=1"),
            new NodeExpectation(32, SegmentContentType.None, "空白", ""),
            new NodeExpectation(33, SegmentContentType.Resource, "钢铁", "industry=5,steel=1"),
            new NodeExpectation(34, SegmentContentType.Event, "备用增援线路", "winter_backup_reinforcement_line"),
            new NodeExpectation(35, SegmentContentType.None, "空白", ""),
            new NodeExpectation(36, SegmentContentType.Resource, "仙人掌果", "cactus_fruit=1,food=10"),
            new NodeExpectation(37, SegmentContentType.Monster, "战术无人机精英", "winter_tactical_drone_elite"),
            new NodeExpectation(38, SegmentContentType.Resource, "燃料", "fuel=1,industry=5"),
            new NodeExpectation(
                39,
                SegmentContentType.Boss,
                "年轻军官·伊莱 + 战术无人机群",
                "young_officer_eli_drone_swarm|winter_tactical_drone_elite")
        };

        [TestCase("spring")]
        [TestCase("summer")]
        [TestCase("fall")]
        [TestCase("winter")]
        public void SeasonLayoutMatchesEveryProvidedNodeAndPayload(string seasonId)
        {
            NodeExpectation[] expected = GetExpectedLayout(seasonId);
            RoadSegmentDefinition[] actual = LoadSeasonSegments(seasonId);

            Assert.That(actual, Has.Length.EqualTo(40), seasonId);
            Assert.That(actual, Has.Length.EqualTo(expected.Length), seasonId);
            Assert.That(
                actual.Select(segment => segment.RoadIndex),
                Is.EqualTo(expected.Select(node => node.RoadIndex)),
                seasonId);
            Assert.That(
                actual.Select(segment => segment.DefinitionId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Is.EqualTo(actual.Length),
                seasonId + " segment definition IDs must be unique");

            for (int index = 0; index < expected.Length; index++)
            {
                NodeExpectation expectedNode = expected[index];
                RoadSegmentDefinition actualNode = actual[index];
                string message = seasonId + " road index " + expectedNode.RoadIndex;

                Assert.That(actualNode.RoadIndex, Is.EqualTo(expectedNode.RoadIndex), message);
                Assert.That(actualNode.ContentType, Is.EqualTo(expectedNode.ContentType), message);
                Assert.That(actualNode.DisplayName, Is.EqualTo(expectedNode.DisplayName), message);
                Assert.That(actualNode.Season, Is.Not.Null, message);
                Assert.That(actualNode.Season.DefinitionId, Is.EqualTo(seasonId), message);
                Assert.That(actualNode.DefinitionId, Is.Not.Null.And.Not.Empty, message);
                Assert.That(BuildPayloadSignature(actualNode, message), Is.EqualTo(expectedNode.Payload), message);
            }
        }

        [TestCase(
            "spring_salt_dust_broken_beacon",
            "connect_beacon[SetFlag:intel_old_broadcast_record:0:true,PushRegionFeed:region_feed:0:true]|remove_core[AddResource:fuel:1:true,AddResource:industry:5:true]")]
        [TestCase(
            "spring_salt_dust_danger_warning",
            "inspect_old_scars[SetFlag:intel_surface_hazard:0:true,PushRegionFeed:region_feed:0:true]|continue_forward[]")]
        [TestCase(
            "spring_salt_dust_karlo_repair_wreck",
            "steady_hatch[AddCharacterFavor:karlo:1:true]|remove_exposed_chip[AddResource:chip:1:true,AddResource:industry:5:true]")]
        [TestCase(
            "spring_salt_dust_marlei_surface_inspection",
            "hear_assessment[SetFlag:intel_salt_crust_risk:0:true,PushRegionFeed:region_feed:0:true]|judge_alone[]")]
        [TestCase(
            "spring_salt_dust_temporary_supply_point",
            "accept_marlei_distribution[AddResource:compressed_biscuit:1:true,AddResource:food:10:true]|prioritize_tools[AddResource:steel:1:true,AddResource:industry:5:true]")]
        public void RestoredSpringEventsKeepTheirChoicePayloads(string definitionId, string expectedSignature)
        {
            GameEventDefinition definition = Resources.LoadAll<GameEventDefinition>("GameData/Events")
                .Single(gameEvent => string.Equals(
                    gameEvent.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));

            Assert.That(BuildEventChoiceSignature(definition), Is.EqualTo(expectedSignature), definitionId);
            Assert.That(
                definition.Choices.All(choice => choice.ConsumeInteractionOnSelect),
                Is.True,
                definitionId);
            Assert.That(
                definition.Choices.All(choice => choice.Conditions.Count == 0),
                Is.True,
                definitionId);
            Assert.That(definition.AutomaticResults, Is.Empty, definitionId);
            Assert.That(
                definition.Choices
                    .SelectMany(choice => choice.Results)
                    .Where(result => result.EffectType == GameEffectType.PushRegionFeed)
                    .All(result => !string.IsNullOrWhiteSpace(result.TextValue)),
                Is.True,
                definitionId + " region feed effects must keep their text payload");
        }

        [TestCase(
            "spring_salt_dust_marlei_karlo_arrival_dialogue",
            "玛蕾、卡洛",
            "两人逃难抵达；玛蕾希望优先收集食物，卡洛不愿离开残骸")]
        [TestCase(
            "spring_salt_dust_graff_marlei_conflict_dialogue",
            "格拉夫、玛蕾",
            "格拉夫出现并主张工业与武器；玛蕾坚持食物与逃生机会")]
        public void RestoredSpringDialoguesKeepTheirContinueChoice(
            string definitionId,
            string expectedSpeaker,
            string expectedBody)
        {
            DialogueDefinition definition = Resources.LoadAll<DialogueDefinition>("GameData/Dialogues")
                .Single(dialogue => string.Equals(
                    dialogue.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));
            DialogueNodeDefinition startNode = definition.StartNode;

            Assert.That(startNode, Is.Not.Null, definitionId);
            Assert.That(startNode.Choices, Has.Count.EqualTo(1), definitionId);

            DialogueChoiceDefinition choice = startNode.Choices[0];
            Assert.That(definition.StartNodeId, Is.EqualTo("start"), definitionId);
            Assert.That(startNode.NodeId, Is.EqualTo("start"), definitionId);
            Assert.That(startNode.SpeakerName, Is.EqualTo(expectedSpeaker), definitionId);
            Assert.That(startNode.BodyText, Is.EqualTo(expectedBody), definitionId);
            Assert.That(choice.ChoiceId, Is.EqualTo("continue"), definitionId);
            Assert.That(choice.ChoiceText, Is.EqualTo("继续"), definitionId);
            Assert.That(choice.ConsumeInteractionOnSelect, Is.True, definitionId);
            Assert.That(choice.ResultType, Is.EqualTo(DialogueChoiceResultType.EndDialogue), definitionId);
        }

        private static RoadSegmentDefinition[] LoadSeasonSegments(string seasonId)
        {
            return Resources.LoadAll<RoadSegmentDefinition>("GameData/RoadSegments")
                .Where(segment => segment.Season != null && string.Equals(
                    segment.Season.DefinitionId,
                    seasonId,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(segment => segment.RoadIndex)
                .ToArray();
        }

        private static NodeExpectation[] GetExpectedLayout(string seasonId)
        {
            switch (seasonId)
            {
                case "spring":
                    return SpringLayout;
                case "summer":
                    return SummerLayout;
                case "fall":
                    return FallLayout;
                case "winter":
                    return WinterLayout;
                default:
                    throw new ArgumentOutOfRangeException(nameof(seasonId), seasonId, "Unknown season ID.");
            }
        }

        private static string BuildPayloadSignature(RoadSegmentDefinition segment, string message)
        {
            switch (segment.ContentType)
            {
                case SegmentContentType.None:
                    Assert.That(segment.Rewards, Is.Empty, message);
                    return string.Empty;

                case SegmentContentType.Monster:
                    Assert.That(segment.Enemy, Is.Not.Null, message);
                    return segment.Enemy == null ? "<missing-enemy>" : segment.Enemy.DefinitionId;

                case SegmentContentType.Npc:
                    Assert.That(segment.Character, Is.Not.Null, message);
                    Assert.That(segment.Dialogue, Is.Not.Null, message);
                    return segment.Dialogue == null ? "<missing-dialogue>" : segment.Dialogue.DefinitionId;

                case SegmentContentType.Resource:
                    Assert.That(segment.Rewards, Is.Not.Empty, message);
                    Assert.That(segment.Rewards.All(reward => reward != null), Is.True, message);
                    return string.Join(
                        ",",
                        segment.Rewards
                            .Where(reward => reward != null)
                            .OrderBy(reward => reward.ResourceId, StringComparer.Ordinal)
                            .Select(reward => reward.ResourceId + "=" + reward.Amount));

                case SegmentContentType.Event:
                    Assert.That(segment.GameEvent, Is.Not.Null, message);
                    return segment.GameEvent == null ? "<missing-event>" : segment.GameEvent.DefinitionId;

                case SegmentContentType.Boss:
                    Assert.That(segment.Boss, Is.Not.Null, message);
                    Assert.That(segment.Enemy, Is.Not.Null, message);
                    return (segment.Boss == null ? "<missing-boss>" : segment.Boss.DefinitionId) +
                           "|" +
                           (segment.Enemy == null ? "<missing-enemy>" : segment.Enemy.DefinitionId);

                default:
                    Assert.Fail(message + " has unsupported content type " + segment.ContentType);
                    return string.Empty;
            }
        }

        private static string BuildEventChoiceSignature(GameEventDefinition definition)
        {
            return string.Join(
                "|",
                definition.Choices.Select(choice =>
                    choice.ChoiceId +
                    "[" +
                    string.Join(",", choice.Results.Select(BuildEffectSignature)) +
                    "]"));
        }

        private static string BuildEffectSignature(GameEffect effect)
        {
            return effect.EffectType +
                   ":" + effect.TargetId +
                   ":" + effect.Amount +
                   ":" + (effect.BoolValue ? "true" : "false");
        }

        private sealed class NodeExpectation
        {
            public NodeExpectation(
                int roadIndex,
                SegmentContentType contentType,
                string displayName,
                string payload)
            {
                RoadIndex = roadIndex;
                ContentType = contentType;
                DisplayName = displayName;
                Payload = payload;
            }

            public int RoadIndex { get; }
            public SegmentContentType ContentType { get; }
            public string DisplayName { get; }
            public string Payload { get; }
        }
    }
}
