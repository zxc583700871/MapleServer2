﻿using System.Xml;
using GameDataParser.Files;
using Maple2.File.IO.Crypto.Common;
using Maple2Storage.Enums;
using Maple2Storage.Types.Metadata;

namespace GameDataParser.Parsers;

public class InteractObjectParser : Exporter<List<InteractObjectMetadata>>
{
    public InteractObjectParser(MetadataResources resources) : base(resources, "interact-object") { }

    protected override List<InteractObjectMetadata> Parse()
    {
        List<InteractObjectMetadata> objects = new();
        foreach (PackFileEntry entry in Resources.XmlReader.Files)
        {
            if (!entry.Name.StartsWith("table/interactobject"))
            {
                continue;
            }

            XmlDocument document = Resources.XmlReader.GetXmlDocument(entry);
            XmlNodeList interactNodes = document.GetElementsByTagName("interact");
            foreach (XmlNode interactNode in interactNodes)
            {
                string locale = interactNode.Attributes["locale"]?.Value ?? "";
                if (locale != "NA" && locale != "")
                {
                    continue;
                }

                InteractObjectMetadata metadata = new()
                {
                    Id = int.Parse(interactNode.Attributes["id"].Value)
                };

                _ = Enum.TryParse(interactNode.Attributes["type"].Value, out metadata.Type);

                foreach (XmlNode childNode in interactNode)
                {
                    switch (childNode.Name)
                    {
                        case "reward":
                            metadata.Reward = new()
                            {
                                Exp = int.Parse(childNode.Attributes["exp"].Value),
                                ExpType = childNode.Attributes["expType"].Value,
                                ExpRate = float.Parse(childNode.Attributes["relativeExpRate"].Value),
                                FirstExpType = childNode.Attributes["firstExpType"].Value,
                                FirstExpRate = float.Parse(childNode.Attributes["firstRelativeExpRate"].Value)
                            };
                            break;
                        case "drop":
                            InteractObjectDropMetadata drop = new()
                            {
                                ObjectLevel = childNode.Attributes["objectLevel"]?.Value ?? "",
                                GlobalDropBoxId = childNode.Attributes["globalDropBoxId"].Value.Split(",").Where(x => !string.IsNullOrEmpty(x)).Select(int.Parse).ToList(),
                                IndividualDropBoxId = childNode.Attributes["individualDropBoxId"].Value.Split(",").Where(x => !string.IsNullOrEmpty(x)).Select(int.Parse).ToList()
                            };
                            _ = int.TryParse(childNode.Attributes["objectDropRank"]?.Value ?? "0", out drop.DropRank);

                            metadata.Drop = drop;
                            break;
                        case "gathering":
                            metadata.Gathering = new()
                            {
                                RecipeId = int.Parse(childNode.Attributes["receipeID"].Value)
                            };
                            break;
                        case "webOpen":
                            metadata.Web = new()
                            {
                                Url = childNode.Attributes["url"].Value
                            };
                            break;
                        case "quest":
                            List<int> questIds = childNode.Attributes["maskQuestID"]?.Value.Split(',', '|').Where(x => !string.IsNullOrEmpty(x)).Select(int.Parse).ToList();
                            List<byte> states = childNode.Attributes["maskQuestState"]?.Value.Split(',', '|').Where(x => !string.IsNullOrEmpty(x)).Select(byte.Parse).ToList();
                            if (questIds == null || states == null)
                            {
                                continue;
                            }

                            for (int i = 0; i < questIds.Count; i++)
                            {
                                if (metadata.Quests.Any(x => x.QuestId == questIds[i]))
                                {
                                    continue;
                                }

                                metadata.Quests.Add((questIds[i], (QuestState) states[i]));
                            }

                            break;
                    }
                }

                objects.Add(metadata);
            }
        }

        return objects;
    }
}
