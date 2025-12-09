@tool
extends Node3D

@export var Tao_San: bool = false:
	set(val): if val: bat_dau_xay()

@export var Xoa_Het: bool = false:
	set(val): if val: don_dep()

func don_dep():
	for con in get_children():
		if con.name != "Slot_1":
			con.queue_free()
	print("🧹 Đã dọn sạch!")

func bat_dau_xay():
	var slot_mau = get_node_or_null("Slot_1")
	if not slot_mau: return

	# Lấy chủ Scene (để lưu file)
	var scene_root = get_tree().edited_scene_root
	var vi_tri_goc = slot_mau.position
	
	print("🏗️ Đang copy toàn bộ cấu trúc...")

	# === 1. XÂY HÀNG PHE TA ===
	for i in range(1, 8):
		var slot_moi = slot_mau.duplicate(15) # Số 15 nghĩa là copy tất cả (Script, Signal, Group...)
		slot_moi.name = "Slot_%d" % (i + 1)
		add_child(slot_moi)
		
		# Đặt vị trí
		slot_moi.position = vi_tri_goc - Vector3(i * 2.0, 0, 0)
		
		# QUAN TRỌNG NHẤT: Gán quyền sở hữu cho Slot mới VÀ tất cả con cái của nó
		gan_quyen_so_huu_toan_bo(slot_moi, scene_root)

	# === 2. XÂY HÀNG PHE ĐỊCH ===
	var z_dich = 18.0
	for i in range(0, 8):
		var slot_dich = slot_mau.duplicate(15)
		slot_dich.name = "Enemy_Slot_%d" % (i + 1)
		add_child(slot_dich)
		
		slot_dich.position = Vector3(vi_tri_goc.x - (i * 2.0), vi_tri_goc.y, z_dich)
		gan_quyen_so_huu_toan_bo(slot_dich, scene_root)
		
		# Đổi màu viền cam
		var vien = slot_dich.get_node_or_null("viendo")
		if not vien: vien = slot_dich.get_child(slot_dich.get_child_count() - 1)
		if vien is MeshInstance3D:
			var mat = StandardMaterial3D.new()
			mat.albedo_color = Color.ORANGE_RED
			mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
			vien.material_override = mat

	print("✅ Xong! Kiểm tra xem các Slot có mũi tên > chưa nhé.")

# Hàm đệ quy: Bắt mọi node con phải khai báo với chủ Scene
func gan_quyen_so_huu_toan_bo(node, root):
	if node != root:
		node.owner = root
	# Duyệt tiếp vào bên trong (con của con của con...)
	for con in node.get_children():
		gan_quyen_so_huu_toan_bo(con, root)
