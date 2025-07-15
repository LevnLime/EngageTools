import xml.etree.ElementTree as ET
import warnings
from pathlib import Path

# Inputs
replaceUniqueClassModels = False  # Set to true if you want to replace character specific unique class models (ex. Archer for Etie, Thief for Yunaka, etc.))

# Format of replacement information is (old model code base, [array of old model extensions], new model code, bIncludeOBody)
# Example: ('Dge0AF', ['c000'], 'Lev0AF_c100', True)
# This will replace instances where the Thief outfit is used (uBody_Dge0AF_c000) with the new model (uBody_Lev0AF_c000)
# Note: Capitalization matters. Searching XML nodes by attribute is case-sensitive.
# bIncludeOBody=True makes it also replace oBody models (map models)
replacementsData = [
    ('Dge0AF', ['c000', 'c699', 'c699d'], 'Lev0AF_c100', True),
    ('Amr0AF', ['c000', 'c699'], 'Lev0AF_c101', True),
    ('Amr1AF', ['c000', 'c699'], 'Lev0AF_c102', True),
    ('Amr2BF', ['c000', 'c699'], 'Lev0AF_c103', True),
    ('Wng0EF', ['c000'], 'Lev0AF_c104', True),
    ('Wng1FF', ['c000'], 'Lev0AF_c105', True),
    ('Wng2DF', ['c000'], 'Lev0AF_c106', True),
    ('Mag0AF', ['c000', 'c000l'], 'Lev0AF_c107', True),
    ('Mag1AF', ['c000', 'c000l', 'c000b', 'c000c', 'c000d', 'c699'], 'Lev0AF_c108', True),
    ('Mag2BF', ['c000', 'c000l'], 'Lev0AF_c109', True),
    ('Swd0AF', ['c000', 'c699'], 'Lev0AF_c110', True),
    ('Swd1AF', ['c000', 'c699'], 'Lev0AF_c111', True),
    ('Swd2AF', ['c000', 'c450'], 'Lev0AF_c112', True),
    ('Axe0AF', ['c000', 'c699'], 'Lev0AF_c113', True),
    ('Axe1AF', ['c000', 'c699', 'c699b', 'c699c', 'c699d', 'c855'], 'Lev0AF_c114', True),
    ('Axe2AF', ['c000', 'c000b', 'c000c', 'c000d'], 'Lev0AF_c115', True),
    ('Rod0AF', ['c000'], 'Lev0AF_c116', True),
    ('Rod1AF', ['c000'], 'Lev0AF_c117', True),
    ('Rod2AF', ['c000'], 'Lev0AF_c118', True),
    ('Bow0AF', ['c000', 'c699'], 'Lev0AF_c119', True),
    ('Bow1AF', ['c000', 'c699'], 'Lev0AF_c120', True),
]

if replaceUniqueClassModels:
    replacementsData += [
        ('Dge0AF', ['c253'], 'Lev0AF_c100', True),  # Yunaka (Thief)
        ('Amr0AF', ['c250'], 'Lev0AF_c101', True),  # Jade (Axe Armor)
        ('Amr1AF', ['c554', 'c554b'], 'Lev0AF_c102', True),  # Marni and Madeline (General)
        ('Wng0EF', ['c153'], 'Lev0AF_c104', True),  # Chloe (Lance Flier)
        ('Wng2DF', ['c303', 'c451'], 'Lev0AF_c106', True),  # Rosado and Seforia (Wyvern Knight)
        ('Mag0AF', ['c252'], 'Lev0AF_c107', True),  # Citrinne (Mage)
        ('Swd0AF', ['c251'], 'Lev0AF_c110', True),  # Lapis (Sword Fighter)
        ('Swd2AF', ['c352'], 'Lev0AF_c112', True),  # Goldmary (Hero)
        ('Axe0AF', ['c552'], 'Lev0AF_c113', True),  # Anna (Axe Fighter)
        ('Axe1AF', ['c453'], 'Lev0AF_c114', True),  # Panette (Berserker)
        ('Axe2AF', ['c254'], 'Lev0AF_c115', True),  # Saphir (Warrior)
        ('Rod0AF', ['c550'], 'Lev0AF_c116', True),  # Framme (Martial Monk)
        ('Rod2AF', ['c151'], 'Lev0AF_c118', True),  # Eve (High Priest)
        ('Bow0AF', ['c152'], 'Lev0AF_c119', True),  # Etie (Archer)
    ]

# Constants
baseXmlFolder = './BaseXml'  # Path to the folder that contains the base (original, unmodified) XML files

# Path to mod folder. If None, this script will put the files in the current working directory.
# Note: If a mod folder is specified, this script will place files in accordance with their location for Cobalt mods.
modFolder = "C:/Users/Burney/AppData/Roaming/Ryujinx/sdcard/engage/mods/SkimpyClassOutfitsT1_OverrideClasses"

# pathlib from Path module lets us use '/' to join paths
inputAssetTablePath = Path(baseXmlFolder) / './base_AssetTable.xml'
outputAssetTablePath = Path(modFolder) / 'patches/xml' / 'AssetTable.xml' if modFolder != None else Path('./AssetTable.xml')

xmlDoc = ET.parse(inputAssetTablePath)
root = xmlDoc.getroot()

# Asset Table structure is like this:
# <Book>
#   <Sheet>
#     <Header>...</Header>
#     <Data>
#       <Param ... />
#       ...more Param elements...
#     </Data>
#   </Sheet>
# </Book>

data = root.find("Sheet/Data")

# Iterate through each replacement entry
for oldModelCodeBase, oldModelCodeExts, newModelCode, bIncludeOBody in replacementsData:
    # There might be multiple model codes to replace. Iterate through each old model code
    for oldModelCodeExt in oldModelCodeExts:
        oldModelCode = f"{oldModelCodeBase}_{oldModelCodeExt}"

        # Replace uBody model. These models are in the "DressModel" attribute of Param elements.
        # Find all Param elements with the specified DressModel attribute
        results = data.findall(f"Param[@DressModel='uBody_{oldModelCode}']")
        # Filter out results that include Conditions="デバッグ用;"
        # "デバッグ用" means "debug use" in Japanese. I'm not sure what they are used for, but we'll exclude them.
        rows = list(filter(lambda x: "デバッグ用;" not in x.get("Conditions"), results))
        # Iterate through the filtered results and replace the DressModel attribute with the new model code
        for row in rows:
            row.set("DressModel", f"uBody_{newModelCode}")
        print(f"uBody_{oldModelCode} -> uBody_{newModelCode} : {len(rows)} rows modified")
        if len(rows) == 0:
            warnings.warn(f"uBody_{oldModelCode} entry not found!")

        # Replace oBody models if option is enabled
        if bIncludeOBody:
            # Find all Param elements with the specified BodyModel attribute
            # Note: I do not observe デバッグ用 entries for oBody models. If that's wrong, then we should filter them
            rows = data.findall(f"Param[@BodyModel='oBody_{oldModelCode}']")
            for row in rows:
                row.set("BodyModel", f"oBody_{newModelCode}")
            print(f"oBody_{oldModelCode} -> oBody_{newModelCode} : {len(rows)} rows modified")
            if len(rows) == 0:
                # Note: Just printing instead of warning because it's possible that there's no oBody model for this entry
                # Example: Thief class has separate uBody models for c000, c699, and c699d, but these share the oBody model for c000
                print(f"oBody_{oldModelCode} entry not found. This might be expected. Please verify.")


# Write the modified XML to a new file
xmlDoc.write(outputAssetTablePath, encoding="utf-8", xml_declaration=True)