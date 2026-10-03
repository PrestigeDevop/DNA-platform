var http=require('http'); 
var postData=JSON.stringify({message:'Hello from smoketest',msgType:'success',title:'Smoke Test'}); 
var req=http.request.apply(null,[{hostname:'127.0.0.1',port:5254,path:'/api/skills/msgbox-alert/execute',method:'POST',headers:{'Content-Type':'application/json','Content-Length':Buffer.byteLength(postData)}},function(res){let d='';res.on('data',function(c){d+=c;});res.on('end',function(){console.log('STATUS:',res.statusCode,'BODY:',d);});}]); 
req.on('error',function(e){console.log('ERROR:',e.message);}); 
req.write(postData);req.end(); 
