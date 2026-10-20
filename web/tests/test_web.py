import importlib.util,json,sys,unittest
from pathlib import Path
from unittest.mock import patch,Mock
ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('handpad_app',ROOT/'app.py'); app=importlib.util.module_from_spec(spec);spec.loader.exec_module(app)
class WebTests(unittest.TestCase):
 def setUp(self):self.client=app.app.test_client()
 def test_pages(self):
  for path in ['/','/quiz','/canvas','/canvas2','/map','/tutorial']+['/page/'+p.stem for p in (ROOT/'pages').glob('*.md')]:
   with self.subTest(path=path):self.assertEqual(self.client.get(path).status_code,200)
 def test_data(self):
  for name in ['canvas','tutorial']:
   self.assertEqual(self.client.get('/api/'+name+'_data').json,json.loads((ROOT/(name+'_data.json')).read_text()))
 def test_quiz_items(self):
  items=json.loads((ROOT/'quiz_data.json').read_text())
  for i in range(len(items)):
   self.assertEqual(self.client.get('/quiz?item='+str(i)).status_code,200)
  self.assertEqual(self.client.post('/quiz?item='+str(len(items)-1)).status_code,200)
 def test_clipboard_and_drag(self):
  self.assertEqual(self.client.post('/api/clipboard',json={'clipboard':'sample'}).status_code,200)
  self.assertEqual(self.client.get('/api/clipboard').json['clipboard'],'sample')
  self.assertEqual(self.client.post('/api/drag',json={'drag_content':'sample','is_dragging':True}).status_code,200)
  self.assertTrue(self.client.get('/api/drag_status').json['is_dragging'])
 def test_source_not_served(self):
  for url in ['/app.py','/requirements.txt','/page/../app.py','/page/../../README']:
   self.assertEqual(self.client.get(url).status_code,404,url)
 def test_static(self):
  for p in (ROOT/'static').rglob('*'):
   if p.is_file():
    with self.client.get('/'+p.relative_to(ROOT/'static').as_posix()) as response:
     self.assertEqual(response.status_code,200)
 def test_image_proxy(self):
  for url in ['http://127.0.0.1','file:///etc/passwd','https://untrusted.example/img.png']:
   self.assertEqual(self.client.get('/fetch_image',query_string={'url':url}).status_code,400)
  response=Mock(status_code=200,headers={'Content-Type':'image/png'},content=b'example')
  with patch.object(app.requests,'get',return_value=response):
   r=self.client.get('/fetch_image',query_string={'url':'https://upload.wikimedia.org/test.png'})
   self.assertEqual(r.status_code,200);self.assertEqual(r.mimetype,'image/png')
if __name__=='__main__':unittest.main(verbosity=2)
