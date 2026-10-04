"""Converte um personagem glTF (.glb) com esqueleto Mixamo para Resources/Modelos/jogador.bytes.
Uso: python3 converter.py Xbot.glb ../../Assets/Camisa10/Resources/Modelos/jogador.bytes
Precisa de numpy."""
import json,struct,numpy as np,sys,os
d=open(sys.argv[1],'rb').read()
l=struct.unpack('<I',d[12:16])[0]; j=json.loads(d[20:20+l])
binoff=20+l; bl=struct.unpack('<I',d[binoff:binoff+4])[0]; BIN=d[binoff+8:binoff+8+bl]
CT={5126:np.float32,5123:np.uint16,5121:np.uint8,5125:np.uint32}
NC={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def acc(i):
    a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]
    off=bv.get('byteOffset',0)+a.get('byteOffset',0); n=NC[a['type']]; dt=CT[a['componentType']]
    stride=bv.get('byteStride',0); isz=np.dtype(dt).itemsize*n
    if stride and stride!=isz:
        raw=np.frombuffer(BIN,np.uint8,a['count']*stride,off).reshape(a['count'],stride)[:,:isz]
        arr=np.frombuffer(raw.tobytes(),dt).reshape(a['count'],n)
    else: arr=np.frombuffer(BIN,dt,a['count']*n,off).reshape(a['count'],n)
    if a.get('normalized'): arr=arr.astype(np.float32)/np.iinfo(dt).max
    return arr
nodes=j['nodes']
parent={}
for i,n in enumerate(nodes):
    for c in n.get('children',[]): parent[c]=i
# bone order: DFS from Armature(69) skipping meshes
order=[]
def dfs(i):
    if 'mesh' in nodes[i]: return
    order.append(i)
    for c in nodes[i].get('children',[]): dfs(c)
dfs(j['scenes'][0]['nodes'][0])
bidx={n:k for k,n in enumerate(order)}
def cpos(p): return np.array([-p[0],p[1],p[2]],np.float32)
def crot(q): return np.array([q[0],-q[1],-q[2],q[3]],np.float32)
rest=[]
for n in order:
    nd=nodes[n]
    rest.append((cpos(nd.get('translation',[0,0,0])),crot(nd.get('rotation',[0,0,0,1])),np.array(nd.get('scale',[1,1,1]),np.float32)))
# matrices
def qm(q):
    x,y,z,w=q
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
def trs(t,r,s):
    M=np.eye(4); M[:3,:3]=qm(r)*s; M[:3,3]=t; return M
def globals_(locs):
    G=[None]*len(order)
    for k,n in enumerate(order):
        L=trs(*locs[k]); p=parent.get(n)
        G[k]=L if p is None or p not in bidx else G[bidx[p]]@L
    return G
Grest=globals_(rest)
skin=j['skins'][0]; joints=skin['joints']; IBM=acc(skin['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1) # column-major
S=np.diag([-1,1,1,1.0])
bind=[np.eye(4) for _ in order]
for ji,n in enumerate(joints): bind[bidx[n]]=S@IBM[ji]@S
# check rest
err=max(np.abs(Grest[bidx[n]]@bind[bidx[n]]-np.eye(4)).max() for n in joints); print('rest check',err)
# meshes
P=[];N=[];J=[];W=[];T=[]
for nd in nodes:
    if 'mesh' not in nd: continue
    for pr in j['meshes'][nd['mesh']]['primitives']:
        at=pr['attributes']; base=len(np.concatenate(P)) if P else 0
        p=acc(at['POSITION']).astype(np.float32)*[-1,1,1]; nn=acc(at['NORMAL']).astype(np.float32)*[-1,1,1]
        jj=acc(at['JOINTS_0']).astype(np.int64); ww=acc(at['WEIGHTS_0']).astype(np.float32)
        jj=np.vectorize(lambda x: bidx[joints[x]])(jj)
        idx=acc(pr['indices']).reshape(-1,3).astype(np.int64)[:,[0,2,1]]+base
        P.append(p);N.append(nn);J.append(jj);W.append(ww);T.append(idx)
P=np.concatenate(P);N=np.concatenate(N);J=np.concatenate(J);W=np.concatenate(W);T=np.concatenate(T)
W=W/W.sum(1,keepdims=True)
# world rest positions of vertices (skinned at rest = same)
names=[nodes[n].get('name','').replace('mixamorig:','') for n in order]
wp=np.array([G[:3,3] for G in Grest])
print('verts',len(P),'tris',len(T),'height', P[:,1].min(),P[:,1].max())
eyeY=wp[names.index('LeftEye')][1]
def cls_of(bone,c):
    b=names[bone]
    if b=='Hips': return 2
    if b in('Spine','Spine1','Spine2') or 'Shoulder' in b or b in('LeftArm','RightArm'): return 0
    if 'ForeArm' in b: return 1
    if 'Hand' in b: return 5
    if b in('Neck',): return 1
    if b in('Head','HeadTop_End','LeftEye','RightEye'): return 6 if c[1]>eyeY+.035*100*0+ .035 else 1
    if 'UpLeg' in b: return 2 if c[1]>wp[names.index('LeftLeg')][1]+ (wp[names.index('LeftUpLeg')][1]-wp[names.index('LeftLeg')][1])*.45 else 1
    if b in('LeftLeg','RightLeg'): return 3
    if 'Foot' in b or 'Toe' in b: return 4
    return 0
cls=np.zeros(len(T),np.int64)
for t,(a,b,c) in enumerate(T):
    acc_={}
    for v in (a,b,c):
        for k in range(4): acc_[J[v,k]]=acc_.get(J[v,k],0)+W[v,k]
    bone=max(acc_,key=acc_.get); cls[t]=cls_of(bone,P[[a,b,c]].mean(0))
print('class counts',np.bincount(cls))
# animations
FPS=30
clips={}
hips=names.index('Hips')
for an in j['animations']:
    if an['name'] not in ('idle','run','walk'): continue
    dur=max(j['accessors'][s['input']]['max'][0] for s in an['samplers']); F=int(round(dur*FPS))+1
    tt=np.linspace(0,dur,F)
    R=np.array([[r[1] for _ in range(F)] for r in rest],np.float32)
    HT=np.array([rest[hips][0]]*F,np.float32)
    for ch in an['channels']:
        n=ch['target']['node']; path=ch['target']['path']
        if n not in bidx: continue
        s=an['samplers'][ch['sampler']]; ti=acc(s['input'])[:,0]; vo=acc(s['output'])
        if path=='rotation':
            out=[]
            for t in tt:
                i=np.searchsorted(ti,t,side='right')-1; i=max(0,min(i,len(ti)-2)) if len(ti)>1 else 0
                if len(ti)==1: q=vo[0]
                else:
                    f=np.clip((t-ti[i])/(ti[i+1]-ti[i]),0,1); q0=vo[i]; q1=vo[i+1]
                    if np.dot(q0,q1)<0: q1=-q1
                    q=q0*(1-f)+q1*f
                q=q/np.linalg.norm(q); out.append(crot(q))
            R[bidx[n]]=np.array(out)
        elif path=='translation' and bidx[n]==hips:
            out=[]
            for t in tt:
                i=np.searchsorted(ti,t,side='right')-1; i=max(0,min(i,len(ti)-2))
                f=np.clip((t-ti[i])/(ti[i+1]-ti[i]),0,1); out.append(cpos(vo[i]*(1-f)+vo[i+1]*f))
            HT=np.array(out,np.float32)
    print(an['name'],'dur',dur,'frames',F,'hips xz drift',HT[:,0].ptp() if hasattr(HT[:,0],'ptp') else np.ptp(HT[:,0]),np.ptp(HT[:,2]))
    HT[:,0]=rest[hips][0][0]; HT[:,2]=rest[hips][0][2]
    clips[an['name']]=(F,R,HT)
if os.environ.get('RIG_NPZ'): np.savez(os.environ['RIG_NPZ'],P=P,N=N,J=J,W=W,T=T,cls=cls,names=np.array(names),parents=np.array([bidx.get(parent.get(n),-1) for n in order]),
  rt=np.array([r[0] for r in rest]),rr=np.array([r[1] for r in rest]),rs=np.array([r[2] for r in rest]),bind=np.array(bind),
  **{f'clip_{k}_R':v[1] for k,v in clips.items()},**{f'clip_{k}_H':v[2] for k,v in clips.items()})
# write binary
def ws(f,s): b=s.encode(); f.write(struct.pack('<i',len(b))); f.write(b)
with open(sys.argv[2],'wb') as f:
    f.write(b'C10R'); f.write(struct.pack('<i',1))
    f.write(struct.pack('<i',len(order)))
    par=[bidx.get(parent.get(n),-1) for n in order]
    for k in range(len(order)):
        ws(f,names[k]); f.write(struct.pack('<i',par[k])); f.write(rest[k][0].astype('<f4').tobytes()); f.write(rest[k][1].astype('<f4').tobytes()); f.write(rest[k][2].astype('<f4').tobytes())
        f.write(np.asarray(bind[k],dtype='<f4').T.tobytes())  # coluna a coluna (m00,m10,m20,m30,...)
    f.write(struct.pack('<i',len(P)))
    f.write(P.astype('<f4').tobytes()); f.write(N.astype('<f4').tobytes())
    f.write(J.astype('<u1').tobytes()); f.write(W.astype('<f4').tobytes())
    NCLS=7; f.write(struct.pack('<i',NCLS))
    for c in range(NCLS):
        tri=T[cls==c].astype('<i4'); f.write(struct.pack('<i',tri.size)); f.write(tri.tobytes())
    f.write(struct.pack('<i',len(clips)))
    for k,(F,R,HT) in clips.items():
        ws(f,k); f.write(struct.pack('<ii',F,FPS)); f.write(R.astype('<f4').tobytes()); f.write(HT.astype('<f4').tobytes())
print('bytes',os.path.getsize(sys.argv[2]))
