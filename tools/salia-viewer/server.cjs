const http=require('node:http');
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'../..');
const port=Number(process.env.SALIA_PORT||4178);
const types={'.html':'text/html; charset=utf-8','.css':'text/css; charset=utf-8','.js':'text/javascript; charset=utf-8','.json':'application/json; charset=utf-8','.png':'image/png','.psd':'image/vnd.adobe.photoshop','.md':'text/plain; charset=utf-8'};
const server=http.createServer((req,res)=>{
  if(req.method!=='GET'&&req.method!=='HEAD'){res.writeHead(405);res.end();return;}
  let pathname;try{pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400);res.end();return;}
  if(pathname==='/'){res.writeHead(302,{Location:'/tools/salia-viewer/'});res.end();return;}
  if(pathname.endsWith('/'))pathname+='index.html';
  const file=path.resolve(root,'.'+pathname);
  const allowed=[path.join(root,'tools/salia-viewer')+path.sep,path.join(root,'assets/title/Character/salia-rig')+path.sep];
  if(!allowed.some(dir=>file.startsWith(dir))){res.writeHead(403);res.end();return;}
  fs.stat(file,(err,stat)=>{if(err||!stat.isFile()){res.writeHead(404);res.end('Not found');return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':'no-cache','X-Content-Type-Options':'nosniff'});if(req.method==='HEAD')res.end();else{const stream=fs.createReadStream(file);stream.on('error',()=>res.destroy());stream.pipe(res);}});
});
server.on('error',err=>{console.error(err.message);process.exitCode=1;});
server.listen(port,'127.0.0.1',()=>console.log(`Salia viewer ready: http://127.0.0.1:${port}/tools/salia-viewer/`));
