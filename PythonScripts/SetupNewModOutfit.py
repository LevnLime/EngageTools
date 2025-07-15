import xml.etree.ElementTree as ET
import xml.dom.minidom
import re
from pathlib import Path

class OutfitData:
    def __init__(self, bundle_code, id=None, name=None, description=None, include_obody=True):
        self.bundle_code = bundle_code
        self.id = id if id is not None else bundle_code
        self.name = name if name is not None else bundle_code
        self.description = description if description is not None else ''
        self.include_obody = include_obody

# Inputs
setupData = [
    OutfitData('Lev0AF_c100', id='SkimpyThiefT1', name='Thief', description='A new outfit for Thief class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c101', id='SkimpyArmorT1', name='Armor', description='A new outfit for Sword/Lance/Axe Armor classes. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c102', id='SkimpyGeneralT1', name='General', description='A new outfit for General class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c103', id='SkimpyGreatKnightT1', name='Great Knight', description='A new outfit for Great Knight class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c104', id='SkimpyPegasusKnightT1', name='Pegasus Knight', description='A new outfit for Sword/Lance/Axe Flier classes. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c105', id='SkimpyGriffinKnightT1', name='Griffin Knight', description='A new outfit for Griffin Knight class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c106', id='SkimpyWyvernKnightT1', name='Wyvern Knight', description='A new outfit for Wyvern Knight class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c107', id='SkimpyMageT1', name='Mage', description='A new outfit for Mage class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c108', id='SkimpySageT1', name='Sage', description='A new outfit for Sage class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c109', id='SkimpyMageKnightT1', name='Mage Knight', description='A new outfit for Mage Knight class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c110', id='SkimpySwordFighterT1', name='Sword Fighter', description='A new outfit for Sword Fighter class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c111', id='SkimpySwordmasterT1', name='Swordmaster', description='A new outfit for Swordmaster class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c112', id='SkimpyHeroT1', name='Hero', description='A new outfit for Hero class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c113', id='SkimpyAxeFighterT1', name='Axe Fighter', description='A new outfit for Axe Fighter class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c114', id='SkimpyBerserkerT1', name='Berserker', description='A new outfit for Berserker class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c115', id='SkimpyWarriorT1', name='Warrior', description='A new outfit for Warrior class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c116', id='SkimpyMartialMonkT1', name='Martial Monk', description='A new outfit for Martial Monk class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c117', id='SkimpyMartialMasterT1', name='Martial Master', description='A new outfit for Martial Master class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c118', id='SkimpyHighPriestT1', name='High Priest', description='A new outfit for High Priest class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c119', id='SkimpyArcherT1', name='Archer', description='A new outfit for Archer class. Part of the Tier 1 skimpy outfits collection.'),
    OutfitData('Lev0AF_c120', id='SkimpySniperT1', name='Sniper', description='A new outfit for Sniper class. Part of the Tier 1 skimpy outfits collection.'),
]

# Path to mod folder. If None, this script will put the files in the current working directory.
# Note: If a mod folder is specified, this script will place files in accordance with their location for Cobalt mods.
modFolder = "C:/Users/Burney/AppData/Roaming/Ryujinx/sdcard/engage/mods/SkimpyClassOutfitsT1"

# Constants
baseXmlFolder = './BaseXml'  # Path to the folder that contains the base (original, unmodified) XML files

# pathlib from Path module lets us use '/' to join paths
baseAssetTableXmlPath = Path(baseXmlFolder) / 'base_AssetTable.xml'
baseItemXmlPath = Path(baseXmlFolder) / 'base_Item.xml'
baseShopXmlPath = Path(baseXmlFolder) / 'base_Shop.xml'

modAssetTableXmlPath = Path(modFolder) / 'patches/xml' / 'AssetTable.xml' if modFolder != None else Path('AssetTable.xml')
modItemXmlPath = Path(modFolder) / 'patches/xml' / 'Item.xml' if modFolder != None else Path('Item.xml')
modShopXmlPath = Path(modFolder) / 'patches/xml' / 'Shop.xml' if modFolder != None else Path('Shop.xml')
modAccessoriesTextPath = Path(modFolder) / 'patches/msbt/message/us/usen' / 'accessories.txt' if modFolder != None else Path('accessories.txt')

def update_item_xml():
    # Item.xml structure is like this:
    # <Book>
    #     <Sheet Name="SheetName">
    #         <Header>
    #             many <Param /> elements
    #         </Header>
    #         <Data>
    #             many <Param /> elements
    #         </Data>
    #     </Sheet>
    # </Book>
    # We are looking for a Sheet named "アクセサリ", which lists "accessories". We'll add our new outfit as an accessory that can be bought.
    xmlDoc = ET.parse(baseItemXmlPath)
    root = xmlDoc.getroot()
    data = root.find('Sheet[@Name="アクセサリ"]/Data')

    for outfit in setupData:
        newNode = create_item_xml_element(outfit)
        data.append(newNode)

    # Save the modified XML to a new file
    write_xml_pretty(xmlDoc, modItemXmlPath)

def update_shop_xml():
    # Shop.xml structure is like this:
    # <Book>
    #     <Sheet Name="SheetName">
    #         <Header>
    #             many <Param /> elements
    #         </Header>
    #         <Data>
    #             many <Param /> elements
    #         </Data>
    #     </Sheet>
    # </Book>
    # We are looking for a Sheet named "アクセサリー屋", which is the Accessories Shop. We'll add our new outfit as an accessory in this shop.
    xmlDoc = ET.parse(baseShopXmlPath)
    root = xmlDoc.getroot()
    data = root.find('Sheet[@Name="アクセサリー屋"]/Data')

    # We want to add our new outfits as early as possible, which means after Chapter 5 when the shop opens.
    # This <Data> element lists the items available in the shop with special elements denoting the chapter they become available.
    # Example:
    # <Data>
    #     <Param Condition="M005" Aid="" />
    #     // <Param ... /> elements for outfits available from Chapter 5
    #     <Param Condition="M007" Aid="" />
    #     // <Param ... /> elements for outfits available from Chapter 7
    #     etc...
    # </Data>
    # We want to insert the new outfits right before <Param Condition="M007" Aid="" />
    # But we'll need to find the index of that first. (For original Shop.xml, this index should be 8)
    startIndex = None
    for i, child in enumerate(data):
        if child.get('Condition') == 'M007':
            startIndex = i
            break
    if startIndex is None:
        raise ValueError('Could not find the <Param Condition="M007" Aid="" /> element for inserting new outfits in Shop.xml!')
    
    # Insert the new outfits before the found index. To keep the original order, we need to insert them in reverse order.
    for outfit in reversed(setupData):
        newNode = create_shop_xml_element(outfit)
        data.insert(startIndex, newNode)

    # Save the modified XML to a new file
    write_xml_pretty(xmlDoc, modShopXmlPath)

def update_asset_table_xml():
    # AssetTable.xml structure is like this:
    # <Book>
    #     <Sheet>
    #         <Header>
    #             many <Param /> elements
    #         </Header>
    #         <Data>
    #             many <Param /> elements
    #         </Data>
    #     </Sheet>
    # </Book>
    # There's just one <Sheet> element here. We'll add our new outfits to the end of the <Data> section.
    xmlDoc = ET.parse(baseAssetTableXmlPath)
    root = xmlDoc.getroot()
    data = root.find('Sheet/Data')

    for outfit in setupData:
        # uBody entry
        uBodyNode = create_asset_table_ubody_element(outfit)
        data.append(uBodyNode)

        if outfit.include_obody:
            # oBody (map model) entry
            oBodyNode = create_asset_table_obody_element(outfit)
            data.append(oBodyNode)
            # Alear hair fix entry
            alearHairFixNode = create_asset_table_alear_hair_fix_element(outfit)
            data.append(alearHairFixNode)

    # Save the modified XML to a new file
    write_xml_pretty(xmlDoc, modAssetTableXmlPath)

def create_accessories_text_file():
    textContentArray = [get_accessories_text_for_outfit(outfit) for outfit in setupData]
    textContent = '\n'.join(textContentArray)

    # Write the text content to a file
    with open(modAccessoriesTextPath, 'w', encoding='utf-8') as f:
        f.write(textContent)

# XML Element Generation
def create_item_xml_element(outfit):
    id = outfit.id
    return ET.Element('Param', {
        'Out': '',
        'Aid': f"AID_{id}",
        'Name': f"MAID_{id}",
        'Help': f"MAID_H_{id}",
        'NameM': '',
        'HelpM': '',
        'NameF': '',
        'HelpF': '',
        'First': 'true',
        'Amiibo': 'false',
        'Asset': '',
        'CondtionCid': '',
        'CondtionSkills': '',
        'CondtionGender': '2',
        'Gid': '',
        'Price': '0',
        'Iron': '0',
        'Steel': '0',
        'Silver': '0',
        'Mask': '1',
    })

def create_shop_xml_element(outfit):
    return ET.Element('Param', {
        'Condition': '',
        'Aid': f"AID_{outfit.id}",
    })

def create_asset_table_ubody_element(outfit):
    return create_asset_table_element({
        "Mode": "2",
        "Conditions": f"AID_{outfit.id};女装;!チキ;!竜化;",
        "DressModel": f"uBody_{outfit.bundle_code}",
        "Comment": f"{outfit.name}",
    })

def create_asset_table_obody_element(outfit):
    return create_asset_table_element({
        "Mode": "1",
        "Conditions": f"AID_{outfit.id};女装;!チキ;!竜化;",
        "BodyModel": f"oBody_{outfit.bundle_code}",
        "Comment": f"{outfit.name} map model",
    })

def create_asset_table_alear_hair_fix_element(outfit):
    # For some strange reason, Alear in Dragon Child / Divine Dragon class does NOT use her normal hair for the map model
    # I have no idea why. But we can fix it by adding a special entry for her.
    # Condition is: Using the outfit AND Alear AND female AND not Tiki/Dragon AND (Class is Dragon Child OR Class is Divine Dragon)
    return create_asset_table_element({
        "Mode": "1",
        "Conditions": f"AID_{outfit.id};MPID_Lueur;女装;!チキ;!竜化;JID_神竜ノ子|JID_神竜ノ王;",
        "HeadModel": "oHair_h051",
        "Comment": f"Alear hair fix for {outfit.name} map model",
    })

def create_asset_table_element(overridesDict):
    element = create_asset_table_empty_element()
    for key, value in overridesDict.items():
        if element.get(key) is not None:
            element.set(key, value)
    return element


# Creates an Asset Table entry without any overrides. This creates an element with all attributes in the default state
def create_asset_table_empty_element():
    return ET.Element('Param', {
        "Out": "",
        "PresetName": "",
        "Mode": "",
        "Conditions": "",
        "BodyModel": "",
        "DressModel": "",
        "MaskColor100R": "0",
        "MaskColor100G": "0",
        "MaskColor100B": "0",
        "MaskColor075R": "0",
        "MaskColor075G": "0",
        "MaskColor075B": "0",
        "MaskColor050R": "0",
        "MaskColor050G": "0",
        "MaskColor050B": "0",
        "MaskColor025R": "0",
        "MaskColor025G": "0",
        "MaskColor025B": "0",
        "HeadModel": "",
        "HairModel": "",
        "HairR": "0",
        "HairG": "0",
        "HairB": "0",
        "GradR": "0",
        "GradG": "0",
        "GradB": "0",
        "SkinR": "0",
        "SkinG": "0",
        "SkinB": "0",
        "ToonR": "0",
        "ToonG": "0",
        "ToonB": "0",
        "RideModel": "",
        "RideDressModel": "",
        "LeftHand": "",
        "RightHand": "",
        "Trail": "",
        "Magic": "",
        "Acc1.Locator": "",
        "Acc1.Model": "",
        "Acc2.Locator": "",
        "Acc2.Model": "",
        "Acc3.Locator": "",
        "Acc3.Model": "",
        "Acc4.Locator": "",
        "Acc4.Model": "",
        "Acc5.Locator": "",
        "Acc5.Model": "",
        "Acc6.Locator": "",
        "Acc6.Model": "",
        "Acc7.Locator": "",
        "Acc7.Model": "",
        "Acc8.Locator": "",
        "Acc8.Model": "",
        "BodyAnim": "",
        "InfoAnim": "",
        "TalkAnim": "",
        "DemoAnim": "",
        "HubAnim": "",
        "ScaleAll": "0",
        "ScaleHead": "0",
        "ScaleNeck": "0",
        "ScaleTorso": "0",
        "ScaleShoulders": "0",
        "ScaleArms": "0",
        "ScaleHands": "0",
        "ScaleLegs": "0",
        "ScaleFeet": "0",
        "VolumeArms": "0",
        "VolumeLegs": "0",
        "VolumeBust": "0",
        "VolumeAbdomen": "0",
        "VolumeTorso": "0",
        "VolumeScaleArms": "0",
        "VolumeScaleLegs": "0",
        "MapScaleAll": "0",
        "MapScaleHead": "0",
        "MapScaleWing": "0",
        "Voice": "",
        "FootStep": "",
        "Material": "",
        "Comment": "",
    })

def get_accessories_text_for_outfit(outfit):
    return f"""\
[MAID_{outfit.id}]
{outfit.name}

[MAID_H_{outfit.id}]
{outfit.description}
"""

# Utility Functions

def write_xml_pretty(xmlDoc, filePath):
    # Convert the XML tree to a string
    rough_string = ET.tostring(xmlDoc.getroot(), encoding='utf-8', xml_declaration=True)
    # Parse the XML again, but using the minidom library
    reparsed = xml.dom.minidom.parseString(rough_string)
    # Convert the DOM object back to a pretty-printed string
    pretty_xml = reparsed.toprettyxml()
    # This "pretty-printed" string contains a lot of empty lines and spaces, so we need to clean it up
    # Clean up the pretty XML string. line.strip() evaluates to False if the line is empty or contains only whitespace, so that filters out empty lines.
    # toprettyxml() also doesn't add a space after the final attribute right before a closing tag, but the original XML does. We're using regex to add it back so that the diff looks clean.
    # Example:
    #   Original  : <Param ... Comment="" />
    #   pretty_xml: <Param ... Comment=""/>
    cleaned_lines = [re.sub("/>$", " />", line) for line in pretty_xml.split("\n") if line.strip()]
    # Fix the first line, which is the XML declaration. (prettyxml omits the encoding, so we want to add it back)
    cleaned_lines[0] = cleaned_lines[0].replace("<?xml version=\"1.0\" ?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>")
    # Join the cleaned lines back into a single string
    cleaned_xml = "\n".join(cleaned_lines)
    # Write the cleaned XML to a file
    with open(filePath, 'w', encoding='utf-8') as f:
        f.write(cleaned_xml)


# Main Execution
update_item_xml()
update_shop_xml()
update_asset_table_xml()
create_accessories_text_file()