import os, platform, shutil, subprocess, tempfile, threading, time, tkinter as tk
from pathlib import Path
from tkinter import messagebox, ttk

DAYS=7
class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Pusula PC Bakım")
        self.geometry("760x590")
        self.minsize(680,540)
        self.configure(bg="#10151f")
        s=ttk.Style(self); s.theme_use("clam")
        s.configure("TFrame",background="#10151f")
        s.configure("TLabel",background="#10151f",foreground="#e8edf5",font=("Segoe UI",10))
        s.configure("Title.TLabel",font=("Segoe UI",22,"bold"),foreground="#ffffff")
        s.configure("Muted.TLabel",foreground="#9ca9bb")
        s.configure("TButton",font=("Segoe UI",10,"bold"),padding=(12,9))
        s.configure("Accent.TButton",background="#55d6be",foreground="#0b1719")
        root=ttk.Frame(self,padding=28); root.pack(fill="both",expand=True)
        ttk.Label(root,text="PUSULA  /  PC BAKIM",style="Muted.TLabel").pack(anchor="w")
        ttk.Label(root,text="Bilgisayarını güvenle toparla.",style="Title.TLabel").pack(anchor="w",pady=(7,5))
        ttk.Label(root,text="Her işlem açıklanır ve onay ister.",style="Muted.TLabel").pack(anchor="w",pady=(0,20))
        cards=ttk.Frame(root); cards.pack(fill="x",pady=(0,18))
        self.osv,self.diskv,self.tempv= tk.StringVar(),tk.StringVar(),tk.StringVar()
        for i,(name,var) in enumerate((("SİSTEM",self.osv),("BOŞ ALAN",self.diskv),("ESKİ GEÇİCİ",self.tempv))):
            c=tk.Frame(cards,bg="#1a2230",padx=14,pady=13); c.grid(row=0,column=i,sticky="nsew",padx=(8 if i else 0,0)); cards.columnconfigure(i,weight=1)
            tk.Label(c,text=name,bg="#1a2230",fg="#9ca9bb",font=("Segoe UI",9,"bold")).pack(anchor="w")
            tk.Label(c,textvariable=var,bg="#1a2230",fg="white",font=("Segoe UI",11,"bold")).pack(anchor="w",pady=(7,0))
        box=tk.Frame(root,bg="#1a2230",padx=18,pady=16); box.pack(fill="x")
        tk.Label(box,text="GÜVENLİ BAKIM ARAÇLARI",bg="#1a2230",fg="white",font=("Segoe UI",11,"bold")).pack(anchor="w",pady=(0,10))
        self.row(box,"Eski geçici dosyaları temizle",f"Sadece kullanıcı geçici klasöründeki {DAYS} günden eski dosyalar; kilitli dosyalar atlanır.","Temizliği başlat",self.clean,True)
        self.row(box,"DNS önbelleğini yenile","Windows DNS önbelleğini temizler. Yönetici izni gerekebilir.","DNS'i yenile",self.dns)
        self.row(box,"Başlangıç uygulamalarını gözden geçir","Windows başlangıç ayarlarını açar; değişiklikleri sen seçersin.","Ayarları aç",self.startup)
        self.status=tk.StringVar(value="Hazır")
        ttk.Label(root,textvariable=self.status,style="Muted.TLabel").pack(anchor="w",pady=(16,4))
        ttk.Label(root,text="Sistem dosyalarına veya kayıt defterine dokunmaz. Hız artışı garantisi vermez.",style="Muted.TLabel").pack(anchor="w")
        self.refresh()

    def row(self,parent,title,detail,label,fn,accent=False):
        r=tk.Frame(parent,bg="#1a2230"); r.pack(fill="x",pady=8)
        t=tk.Frame(r,bg="#1a2230"); t.pack(side="left",fill="x",expand=True)
        tk.Label(t,text=title,bg="#1a2230",fg="#e8edf5",font=("Segoe UI",10,"bold")).pack(anchor="w")
        tk.Label(t,text=detail,bg="#1a2230",fg="#9ca9bb",font=("Segoe UI",9),wraplength=430,justify="left").pack(anchor="w",pady=(3,0))
        ttk.Button(r,text=label,command=fn,style="Accent.TButton" if accent else "TButton").pack(side="right",padx=(10,0))

    @staticmethod
    def temp_root(): return Path(tempfile.gettempdir()).resolve()

    @staticmethod
    def size_text(n):
        v=float(n)
        for unit in ("B","KB","MB","GB"):
            if v<1024 or unit=="GB": return f"{v:.1f} {unit}"
            v/=1024

    def candidates(self):
        total=count=0; cutoff=time.time()-DAYS*86400
        try:
            for here,dirs,files in os.walk(self.temp_root(),followlinks=False):
                dirs[:]=[d for d in dirs if not (Path(here)/d).is_symlink()]
                for name in files:
                    p=Path(here)/name
                    try:
                        if not p.is_symlink():
                            st=p.stat()
                            if st.st_mtime<cutoff: total+=st.st_size; count+=1
                    except OSError: pass
        except OSError: pass
        return total,count

    def refresh(self):
        self.osv.set(f"{platform.system()} {platform.release()}")
        try: self.diskv.set(f"{shutil.disk_usage(Path.home().anchor or 'C:\\\\').free/1024**3:.1f} GB boş")
        except OSError: self.diskv.set("Bilgi alınamadı")
        n,c=self.candidates(); self.tempv.set(f"{c} dosya · {self.size_text(n)}")

    def clean(self):
        n,c=self.candidates()
        if not c: return messagebox.showinfo("Pusula","7 günden eski temizlenebilir dosya bulunamadı.")
        if not messagebox.askyesno("Pusula",f"{c} eski dosya ({self.size_text(n)}) silinsin mi?\nKilitli dosyalar atlanır."): return
        self.status.set("Temizleniyor…"); threading.Thread(target=self.clean_worker,daemon=True).start()

    def clean_worker(self):
        cutoff=time.time()-DAYS*86400; deleted=freed=skipped=0
        try:
            for here,dirs,files in os.walk(self.temp_root(),topdown=False,followlinks=False):
                dirs[:]=[d for d in dirs if not (Path(here)/d).is_symlink()]
                for name in files:
                    p=Path(here)/name
                    try:
                        if not p.is_symlink() and p.stat().st_mtime<cutoff:
                            size=p.stat().st_size; p.unlink(); deleted+=1; freed+=size
                    except OSError: skipped+=1
        except OSError: skipped+=1
        self.after(0,lambda:self.done(deleted,freed,skipped))

    def done(self,d,f,s):
        self.status.set(f"Temizlik tamamlandı · {d} dosya · {self.size_text(f)}"); self.refresh()
        messagebox.showinfo("Pusula",f"Silinen: {d} dosya ({self.size_text(f)})\nAtlanan: {s}")

    def dns(self):
        if not messagebox.askyesno("Pusula","DNS önbelleği temizlensin mi?"):
            return
        try:
            r=subprocess.run(["ipconfig","/flushdns"],capture_output=True,text=True,timeout=15,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
            msg=r.stdout.strip() or r.stderr.strip() or f"Çıkış kodu: {r.returncode}"
            self.status.set(msg.splitlines()[-1][:120]); messagebox.showinfo("Pusula",msg[-1000:])
        except (OSError,subprocess.TimeoutExpired) as e: messagebox.showerror("Pusula",str(e))

    def startup(self):
        try: os.startfile("ms-settings:startupapps")
        except (AttributeError,OSError) as e: messagebox.showerror("Pusula",str(e))

if __name__=="__main__":
    if platform.system()!="Windows": raise SystemExit("Yalnızca Windows desteklenir.")
    App().mainloop()

