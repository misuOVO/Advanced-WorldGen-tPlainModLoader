using System;
using System.Collections.Generic;

namespace AdvancedWorldGen
{
    internal sealed class SeedCombo
    {
        public string[] Keys;
        public string Hint;
    }

    /// <summary>
    /// Terraria Wiki「特殊种子组合」页里的主要组合。
    /// 键名对应原版 WorldGenerationOptions 的选项（Drunk / Anniversary / ...）。
    /// </summary>
    internal static class SeedCombos
    {
        public static readonly List<SeedCombo> All = new List<SeedCombo>
        {
            new SeedCombo { Keys = new[] { "Drunk", "ForTheWorthy" },
                Hint = "巨石雨；猩红与腐化洞穴的地形风格互换。" },

            new SeedCombo { Keys = new[] { "Drunk", "DontStarve" },
                Hint = "云朵变成饥荒样式；机械师代替向导作为起始 NPC；月亮会定位在 logo 下方。" },

            new SeedCombo { Keys = new[] { "Drunk", "Remix" },
                Hint = "播放异界版太空音乐；两种邪恶生物群系分别生成在世界两侧；地牢被涂成深色，入口是一棵枯萎的生命树。" },

            new SeedCombo { Keys = new[] { "Drunk", "DontStarve", "Anniversary" },
                Hint = "丛林生成在地牢同侧、雪原在对侧（与正常世界相反）；丛林神庙通常生成在丛林外。" },

            new SeedCombo { Keys = new[] { "Drunk", "DontStarve", "Remix" },
                Hint = "丛林和雪原生物群系都会生成在地牢同侧。" },

            new SeedCombo { Keys = new[] { "Drunk", "Anniversary" },
                Hint = "开局自动开始派对；出生点一侧的液体变成微光；可以无守卫进入地牢；生命树地牢入口不再生成。" },

            new SeedCombo { Keys = new[] { "Anniversary", "DontStarve" },
                Hint = "草地和生命树不染色，但生成的树木会染色；特定 NPC 开局派对；出生点位于地图中心；漂浮岛会生成。" },

            new SeedCombo { Keys = new[] { "Anniversary", "NotTheBees" },
                Hint = "地形不染色但树木染色；海洋是水而不是蜂蜜；地牢涂成蜂巢配色；漂浮岛带普通草。" },

            new SeedCombo { Keys = new[] { "Anniversary", "ForTheWorthy" },
                Hint = "移除世界生成的神圣；所有 NPC 第一天后死亡；漂浮岛带粉色云；机械骷髅王不再破坏地形。" },

            new SeedCombo { Keys = new[] { "Anniversary", "Remix" },
                Hint = "不存在献给配得上的人时，地表完全由神圣生成；漂浮岛覆盖规则；微光/蜂蜜/熔岩会覆盖不同区域。" },

            new SeedCombo { Keys = new[] { "Anniversary", "Remix", "ForTheWorthy" },
                Hint = "金字塔和生命树会按世界邪恶生物群系的颜色涂色。" },

            new SeedCombo { Keys = new[] { "Anniversary", "NoTraps" },
                Hint = "彩虹巨石会变得常见得多。" },

            new SeedCombo { Keys = new[] { "ForTheWorthy", "Remix" },
                Hint = "机械美杜莎代替机械 Boss；仙人掌主题地下小屋；困难模式世界更新立即启用。" },

            new SeedCombo { Keys = new[] { "DontStarve", "ForTheWorthy" },
                Hint = "克苏鲁之眼和机械 Boss 会重复生成；1 颗暗影珠/猩红之心即可召唤邪恶 Boss；暴雨会下陨石雨；下雨会积水。" },

            new SeedCombo { Keys = new[] { "DontStarve", "Remix" },
                Hint = "饥饿机制的每个阶段持续时间延长 60%（5 分钟 → 8 分钟）。" },

            new SeedCombo { Keys = new[] { "NoTraps", "NotTheBees" },
                Hint = "丛林神庙机关（超级飞镖陷阱、烈焰陷阱）会替换普通飞镖陷阱。" },

            new SeedCombo { Keys = new[] { "NoTraps", "ForTheWorthy" },
                Hint = "所有爆炸物的引爆时间缩短一半；可以生成尖刺山洞。" },

            new SeedCombo { Keys = new[] { "Skyblock", "ForTheWorthy" },
                Hint = "移除微光岛，只留下初始岛；初始岛上的固化机也会被移除。" },

            new SeedCombo { Keys = new[] { "Skyblock", "Everything" },
                Hint = "两座岛都生成在地狱；以太岛和固化机仍然存在。" },

            new SeedCombo { Keys = new[] { "Skyblock", "Drunk" },
                Hint = "起始 NPC 变为派对女孩，而不是向导。" },

            new SeedCombo { Keys = new[] { "Skyblock", "Anniversary" },
                Hint = "云块会被涂成粉色。" },

            new SeedCombo { Keys = new[] { "Skyblock", "NoTraps" },
                Hint = "飞镖陷阱史莱姆生成率提高，可以在地表生成，并会放置几个陷阱。" },

            new SeedCombo { Keys = new[] { "Skyblock", "Remix" },
                Hint = "初始岛位于地狱；云会被替换成病变块或血肉块。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "ForTheWorthy" },
                Hint = "地狱覆盖蜘蛛窝；松脆蜂蜜块会灼烧玩家并释放熔岩；废墟建筑由蜂蜜块构成。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "Skyblock" },
                Hint = "初始岛由泥块、蜂巢和雨云组成。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "Remix" },
                Hint = "松脆蜂蜜块出现在熔岩层；蜂蜜以池塘形式出现在地表；地下层大量蜂巢与松脆蜂蜜块。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "DontStarve" },
                Hint = "绿洲和雪原生物群系可以生成；蜂蜜不会替换水；两个海洋保持原样；向导仍是起始 NPC。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "Drunk" },
                Hint = "播放丛林异界版音乐；丛林神庙墙涂成深橙绿漆；小世界可能因为没有蜂巢而缺少蜂巢生物群系。" },

            new SeedCombo { Keys = new[] { "NotTheBees", "Drunk", "DontStarve" },
                Hint = "出生点所在的世界中心是一个森林生物群系。" },
        };
    }
}
