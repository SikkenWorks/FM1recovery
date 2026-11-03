"""Build combined native icon and UAC manifest resources for the Mono build."""
from pathlib import Path
import struct
root=Path(__file__).resolve().parent
pad=lambda b:b+b'\0'*((-len(b))%4)
def resource(kind, ident, data):
    rest=struct.pack('<HHHHIHHII',65535,kind,65535,ident,0,0x1030,1033,0,0)
    return struct.pack('<II',len(data),8+len(rest))+rest+pad(data)
blob=struct.pack('<IIHHHHIHHII',0,32,65535,0,65535,0,0,0,0,0,0)
ico=(root/'app.ico').read_bytes();reserved,kind,count=struct.unpack_from('<HHH',ico)
group=struct.pack('<HHH',reserved,kind,count)
for i in range(count):
    w,h,colors,res,planes,bits,size,offset=struct.unpack_from('<BBBBHHII',ico,6+16*i)
    blob+=resource(3,i+1,ico[offset:offset+size])
    group+=struct.pack('<BBBBHHIH',w,h,colors,res,planes,bits,size,i+1)
blob+=resource(14,1,group)
blob+=resource(24,1,(root/'app.manifest').read_bytes())
(root/'native.res').write_bytes(blob)
