from pathlib import Path
import difflib, hashlib, re
stage=Path(__file__).parent
project=Path('D:/Unity Projects/Circle_War')
for index in ('05','19','32','33'):
    before=(stage/f'SummerLowHumidityShore{index}.before.asset').read_text(encoding='utf-8-sig')
    after=(project/f'Assets/Resources/GameData/RoadSegments/SummerLowHumidityShore{index}.asset').read_text(encoding='utf-8-sig')
    changes=[s for s in difflib.ndiff(before.splitlines(),after.splitlines()) if s.startswith(('- ','+ '))]
    assert len(changes)==2 and all('offset: {x:' in s for s in changes),changes
    print(index, *changes, sep='\n')
style_before=(stage/'SummerPixelArtStyle.before.asset').read_text(encoding='utf-8-sig')
style_after=(project/'Assets/Resources/ArtStyles/SummerPixelArtStyle.asset').read_text(encoding='utf-8-sig')
changes=[s for s in difflib.ndiff(style_before.splitlines(),style_after.splitlines()) if s.startswith(('- ','+ '))]
assert len(changes)==6 and all('pixel:' in s for s in changes),changes
print('Style: exactly 3 pixel references changed; palettes and contacts preserved.')
expected={
'Assets/Scenes/SummerPixelPreview.unity':'3852C7EE8A9E5BD7DA6A6303F106FCCBBE9A83CE08F1E947432B11CBE9BC5C71',
'Assets/Scenes/SpringPixelPreview.unity':'112826C69E60DA8EF8CDF752CA1FB93B036422CF1F4B1C52AEF57434E583C17C',
'Assets/Scenes/SampleScene.unity':'54D0919C43AE33177E037238E7FEC4214BA75B9AE49DA5E96FC597DAA52A9169',
'Assets/Resources/ArtStyles/SpringPixelArtStyle.asset':'27B0676B65202BC6186F3459724F3C7E68D2544BDAB172D05B5A8764D2D6EF44',
'Assets/Resources/ArtStyles/CircleGroundPixelDensity.mat':'387F0CBE4216DC10C1DDFC068BACEF2773B18568635E78A21119FDFD19FB6EE5'}
for path,value in expected.items():
    digest=hashlib.sha256((project/path).read_bytes()).hexdigest().upper()
    assert digest==value,path
    print('Preserved:',path)
