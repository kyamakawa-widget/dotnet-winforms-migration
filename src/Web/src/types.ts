export interface Category {
  id: number;
  name: string;
}

export interface CreateOrderRequest {
  orderNo: string;
  customerName: string;
  categoryId: number;
  itemName: string;
  price: number;
  qty: number;
}
