extends CharacterBody3D

# --- CHỈ SỐ SỨC MẠNH ---
@export var mau_toi_da: int = 100
var mau_hien_tai: int = 0
@export var sat_thuong: int = 10
@export var toc_do_danh: float = 1.0 # Giây/nhát
@export var toc_do_chay: float = 4.0
@export var tam_danh: float = 1.5

# --- TRẠNG THÁI ---
var dang_chien_dau: bool = false
var muc_tieu: Node3D = null # Kẻ địch đang nhắm tới
var thoi_gian_hoi_chieu: float = 0.0

# Biến để GameManager nhận diện (quan trọng)
var tren_san_dau: bool = false 

func _ready():
	mau_hien_tai = mau_toi_da
	
	# Tự động phân loại phe dựa vào Group
	# Nếu chưa có Group thì tự thêm vào DongMinh (mặc định)
	if not is_in_group("DongMinh") and not is_in_group("KeThu"):
		add_to_group("DongMinh")
		
	# Tạo thanh máu trên đầu (nếu muốn)
	tao_thanh_mau_tam_thoi()

func _process(delta):
	# Nếu chưa vào trận hoặc bị chết -> Không làm gì cả
	if not dang_chien_dau or mau_hien_tai <= 0:
		return
	
	# Hồi chiêu đánh
	if thoi_gian_hoi_chieu > 0:
		thoi_gian_hoi_chieu -= delta

	# 1. Nếu chưa có mục tiêu hoặc mục tiêu đã chết -> Tìm thằng khác
	if not is_instance_valid(muc_tieu) or muc_tieu.mau_hien_tai <= 0:
		tim_ke_dich_gan_nhat()
		return
	
	# 2. Tính khoảng cách tới mục tiêu
	var khoang_cach = global_position.distance_to(muc_tieu.global_position)
	
	if khoang_cach > tam_danh:
		# --- DI CHUYỂN ---
		# Hướng mặt về phía địch
		look_at(muc_tieu.global_position, Vector3.UP)
		rotation.x = 0 # Giữ người thẳng đứng
		rotation.z = 0
		
		# Lao tới
		var huong = (muc_tieu.global_position - global_position).normalized()
		velocity = huong * toc_do_chay
		move_and_slide()
	else:
		# --- TẤN CÔNG ---
		velocity = Vector3.ZERO # Đứng lại
		if thoi_gian_hoi_chieu <= 0:
			tan_cong_muc_tieu()

# --- HÀM CHIẾN ĐẤU ---
func vao_tran():
	if tren_san_dau:
		dang_chien_dau = true
		print(name, ": GAAAA! Xông lên!")

func tim_ke_dich_gan_nhat():
	var nhom_dich = "KeThu" if is_in_group("DongMinh") else "DongMinh"
	var danh_sach_dich = get_tree().get_nodes_in_group(nhom_dich)
	
	var khoang_cach_ngan_nhat = 9999.0
	var dich_gan_nhat = null
	
	for dich in danh_sach_dich:
		if dich.mau_hien_tai > 0 and dich.tren_san_dau: # Chỉ đánh đứa còn sống và đang trên sân
			var kc = global_position.distance_to(dich.global_position)
			if kc < khoang_cach_ngan_nhat:
				khoang_cach_ngan_nhat = kc
				dich_gan_nhat = dich
	
	muc_tieu = dich_gan_nhat

func tan_cong_muc_tieu():
	# Reset hồi chiêu
	thoi_gian_hoi_chieu = toc_do_danh
	
	# Animation nhảy lên 1 chút (giả vờ đánh)
	var tween = create_tween()
	tween.tween_property(self, "scale", Vector3(1.2, 1.2, 1.2), 0.1)
	tween.tween_property(self, "scale", Vector3(1, 1, 1), 0.1)
	
	print(name, " chém ", muc_tieu.name)
	
	# Gây sát thương
	if muc_tieu.has_method("nhan_sat_thuong"):
		muc_tieu.nhan_sat_thuong(sat_thuong)

func nhan_sat_thuong(dam):
	mau_hien_tai -= dam
	print(name, " bị đau! Máu còn: ", mau_hien_tai)
	
	# Cập nhật thanh máu (nếu có)
	cap_nhat_thanh_mau()
	
	if mau_hien_tai <= 0:
		chet()

func chet():
	print(name, " đã hy sinh!")
	# Xóa khỏi game
	queue_free()

# --- PHỤ TRỢ: THANH MÁU ---
var label3d_mau: Label3D
func tao_thanh_mau_tam_thoi():
	label3d_mau = Label3D.new()
	label3d_mau.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label3d_mau.position = Vector3(0, 2.5, 0) # Cao hơn đầu 2.5m
	label3d_mau.text = str(mau_hien_tai)
	label3d_mau.font_size = 64
	label3d_mau.modulate = Color.GREEN if is_in_group("DongMinh") else Color.RED
	add_child(label3d_mau)

func cap_nhat_thanh_mau():
	if label3d_mau:
		label3d_mau.text = str(mau_hien_tai)
